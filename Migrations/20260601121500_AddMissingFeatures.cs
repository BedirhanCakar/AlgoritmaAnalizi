using System;
using ExamScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamScheduler.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260601121500_AddMissingFeatures")]
    public partial class AddMissingFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Derslikler Tablosuna Kat Alanı Ekleme
            migrationBuilder.AddColumn<int>(
                name: "Kat",
                table: "Derslikler",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 2. SQL Server Seviyesinde Login'lerin Oluşturulması (İşlem Dışı Çalıştırılır)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'AppAdminLogin')
                BEGIN
                    CREATE LOGIN AppAdminLogin WITH PASSWORD = 'AdminPassword123!', CHECK_POLICY = OFF;
                END", suppressTransaction: true);

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'AppViewerLogin')
                BEGIN
                    CREATE LOGIN AppViewerLogin WITH PASSWORD = 'ViewerPassword123!', CHECK_POLICY = OFF;
                END", suppressTransaction: true);

            // 3. Veritabanı Kullanıcılarının ve Rol Atamalarının Yapılması
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'AppAdminUser')
                BEGIN
                    CREATE USER AppAdminUser FOR LOGIN AppAdminLogin;
                END
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'AppViewerUser')
                BEGIN
                    CREATE USER AppViewerUser FOR LOGIN AppViewerLogin;
                END

                -- Yetkilendirme
                ALTER ROLE db_owner ADD MEMBER AppAdminUser;
                ALTER ROLE db_datareader ADD MEMBER AppViewerUser;
            ");

            // 4. SP_AkilliSalonHesapla Prosedürünün Kat Optimizasyonu ile Güncellenmesi
            migrationBuilder.Sql(@"
                ALTER PROCEDURE SP_AkilliSalonHesapla
                    @SinavId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @OgrenciSayisi INT;
                    SELECT @OgrenciSayisi = d.OgrenciSayisi FROM Sinavlar s JOIN Dersler d ON s.DersId = d.Id WHERE s.Id = @SinavId;
                    
                    DECLARE @AtananKapasite INT = dbo.FN_SinavToplamKapasite(@SinavId);
                    
                    IF @AtananKapasite >= @OgrenciSayisi
                    BEGIN
                        PRINT 'Yeterli kapasite zaten atanmış.';
                        RETURN;
                    END

                    DECLARE @TargetBaslangic TIME, @TargetBitis TIME, @TargetTarih DATE;
                    SELECT @TargetBaslangic = o.BaslangicSaat, @TargetBitis = o.BitisSaat, @TargetTarih = CAST(s.Tarih AS DATE)
                    FROM Sinavlar s JOIN Oturumlar o ON s.OturumId = o.Id WHERE s.Id = @SinavId;

                    -- İlk (en büyük kapasiteli) boş dersliği bulalım
                    DECLARE @FirstDerslikId INT, @FirstKapasite INT, @FirstKat INT;
                    SELECT TOP 1 @FirstDerslikId = Id, @FirstKapasite = Kapasite, @FirstKat = Kat
                    FROM Derslikler d
                    WHERE Aktif = 1 AND NOT EXISTS (
                        SELECT 1 FROM SinavSalonlari ss2 
                        JOIN Sinavlar s2 ON ss2.SinavId = s2.Id
                        JOIN Oturumlar o2 ON s2.OturumId = o2.Id
                        WHERE ss2.DerslikId = d.Id AND CAST(s2.Tarih AS DATE) = @TargetTarih
                        AND o2.BaslangicSaat < @TargetBitis
                        AND o2.BitisSaat > @TargetBaslangic
                    )
                    ORDER BY Kapasite DESC;

                    -- Eğer hiç boş derslik yoksa çık
                    IF @FirstDerslikId IS NULL
                        RETURN;

                    -- İlk dersliği ata
                    INSERT INTO SinavSalonlari (SinavId, DerslikId) VALUES (@SinavId, @FirstDerslikId);
                    SET @AtananKapasite = @AtananKapasite + @FirstKapasite;

                    -- Eğer daha fazla kapasite gerekiyorsa, diğer derslikleri ilk dersliğin katına yakınlığına göre seç
                    IF @AtananKapasite < @OgrenciSayisi
                    BEGIN
                        DECLARE cur CURSOR FOR 
                        SELECT Id, Kapasite FROM Derslikler d
                        WHERE Aktif = 1 AND Id <> @FirstDerslikId AND NOT EXISTS (
                            SELECT 1 FROM SinavSalonlari ss2 
                            JOIN Sinavlar s2 ON ss2.SinavId = s2.Id
                            JOIN Oturumlar o2 ON s2.OturumId = o2.Id
                            WHERE ss2.DerslikId = d.Id AND CAST(s2.Tarih AS DATE) = @TargetTarih
                            AND o2.BaslangicSaat < @TargetBitis
                            AND o2.BitisSaat > @TargetBaslangic
                        )
                        -- Kat yakınlığı öncelikli, ardından kapasite büyükten küçüğe
                        ORDER BY ABS(Kat - @FirstKat) ASC, Kapasite DESC;

                        DECLARE @DerslikId INT, @Kapasite INT;
                        OPEN cur;
                        FETCH NEXT FROM cur INTO @DerslikId, @Kapasite;
                        
                        WHILE @@FETCH_STATUS = 0 AND @AtananKapasite < @OgrenciSayisi
                        BEGIN
                            INSERT INTO SinavSalonlari (SinavId, DerslikId) VALUES (@SinavId, @DerslikId);
                            SET @AtananKapasite = @AtananKapasite + @Kapasite;
                            FETCH NEXT FROM cur INTO @DerslikId, @Kapasite;
                        END
                        
                        CLOSE cur;
                        DEALLOCATE cur;
                    END
                END;
            ");

            // 5. SP_GozetmenHavuzAtama Prosedürünün Ardışık Oturum Kısıtıyla Güncellenmesi
            migrationBuilder.Sql(@"
                ALTER PROCEDURE SP_GozetmenHavuzAtama
                    @SinavSalonuId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    DECLARE @SinavId INT, @Tarih DATE, @OturumId INT;
                    SELECT @SinavId = s.Id, @Tarih = CAST(s.Tarih AS DATE), @OturumId = s.OturumId 
                    FROM SinavSalonlari ss JOIN Sinavlar s ON ss.SinavId = s.Id WHERE ss.Id = @SinavSalonuId;

                    -- Hedef oturumun gün içindeki sırasını belirleyelim
                    DECLARE @TargetRowNum INT;
                    WITH DaySessions AS (
                        SELECT Id, ROW_NUMBER() OVER (ORDER BY BaslangicSaat) AS RowNum
                        FROM Oturumlar
                    )
                    SELECT @TargetRowNum = RowNum FROM DaySessions WHERE Id = @OturumId;

                    DECLARE @PersonelId INT;
                    
                    -- Müsait olan, çakışması olmayan ve ardışık 3'ten fazla görev almayacak en az görevli personeli bulur
                    SELECT TOP 1 @PersonelId = p.Id
                    FROM Personeller p
                    WHERE dbo.FN_PersonelMusaitMi(p.Id, @Tarih) = 1
                    AND NOT EXISTS (
                        -- Aynı oturumda başka görevi olmamalı
                        SELECT 1 FROM GozetmenAtamalari ga2
                        JOIN SinavSalonlari ss2 ON ga2.SinavSalonuId = ss2.Id
                        JOIN Sinavlar s2 ON ss2.SinavId = s2.Id
                        WHERE ga2.PersonelId = p.Id AND s2.OturumId = @OturumId AND CAST(s2.Tarih AS DATE) = @Tarih
                    )
                    AND NOT EXISTS (
                        -- Hedef oturuma atandığında ardışık 4 oturum oluşmasını engelle
                        SELECT 1
                        WHERE 
                            -- 1. Durum: N-3, N-2, N-1 zaten atanmış
                            (
                                (SELECT COUNT(*) FROM GozetmenAtamalari ga3 
                                 JOIN SinavSalonlari ss3 ON ga3.SinavSalonuId = ss3.Id
                                 JOIN Sinavlar s3 ON ss3.SinavId = s3.Id
                                 JOIN Oturumlar o3 ON s3.OturumId = o3.Id
                                 WHERE ga3.PersonelId = p.Id AND CAST(s3.Tarih AS DATE) = @Tarih
                                 AND o3.Id IN (
                                     SELECT Id FROM (SELECT Id, ROW_NUMBER() OVER (ORDER BY BaslangicSaat) AS RN FROM Oturumlar) t 
                                     WHERE RN IN (@TargetRowNum - 3, @TargetRowNum - 2, @TargetRowNum - 1)
                                 )
                                ) = 3
                            )
                            OR
                            -- 2. Durum: N-2, N-1, N+1 zaten atanmış
                            (
                                (SELECT COUNT(*) FROM GozetmenAtamalari ga3 
                                 JOIN SinavSalonlari ss3 ON ga3.SinavSalonuId = ss3.Id
                                 JOIN Sinavlar s3 ON ss3.SinavId = s3.Id
                                 JOIN Oturumlar o3 ON s3.OturumId = o3.Id
                                 WHERE ga3.PersonelId = p.Id AND CAST(s3.Tarih AS DATE) = @Tarih
                                 AND o3.Id IN (
                                     SELECT Id FROM (SELECT Id, ROW_NUMBER() OVER (ORDER BY BaslangicSaat) AS RN FROM Oturumlar) t 
                                     WHERE RN IN (@TargetRowNum - 2, @TargetRowNum - 1, @TargetRowNum + 1)
                                 )
                                ) = 3
                            )
                            OR
                            -- 3. Durum: N-1, N+1, N+2 zaten atanmış
                            (
                                (SELECT COUNT(*) FROM GozetmenAtamalari ga3 
                                 JOIN SinavSalonlari ss3 ON ga3.SinavSalonuId = ss3.Id
                                 JOIN Sinavlar s3 ON ss3.SinavId = s3.Id
                                 JOIN Oturumlar o3 ON s3.OturumId = o3.Id
                                 WHERE ga3.PersonelId = p.Id AND CAST(s3.Tarih AS DATE) = @Tarih
                                 AND o3.Id IN (
                                     SELECT Id FROM (SELECT Id, ROW_NUMBER() OVER (ORDER BY BaslangicSaat) AS RN FROM Oturumlar) t 
                                     WHERE RN IN (@TargetRowNum - 1, @TargetRowNum + 1, @TargetRowNum + 2)
                                 )
                                ) = 3
                            )
                            OR
                            -- 4. Durum: N+1, N+2, N+3 zaten atanmış
                            (
                                (SELECT COUNT(*) FROM GozetmenAtamalari ga3 
                                 JOIN SinavSalonlari ss3 ON ga3.SinavSalonuId = ss3.Id
                                 JOIN Sinavlar s3 ON ss3.SinavId = s3.Id
                                 JOIN Oturumlar o3 ON s3.OturumId = o3.Id
                                 WHERE ga3.PersonelId = p.Id AND CAST(s3.Tarih AS DATE) = @Tarih
                                 AND o3.Id IN (
                                     SELECT Id FROM (SELECT Id, ROW_NUMBER() OVER (ORDER BY BaslangicSaat) AS RN FROM Oturumlar) t 
                                     WHERE RN IN (@TargetRowNum + 1, @TargetRowNum + 2, @TargetRowNum + 3)
                                 )
                                ) = 3
                            )
                    )
                    ORDER BY (SELECT COUNT(*) FROM GozetmenAtamalari WHERE PersonelId = p.Id) ASC;

                    IF @PersonelId IS NOT NULL
                    BEGIN
                        INSERT INTO GozetmenAtamalari (SinavSalonuId, PersonelId) VALUES (@SinavSalonuId, @PersonelId);
                    END
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kat",
                table: "Derslikler");

            migrationBuilder.Sql(@"
                ALTER ROLE db_owner DROP MEMBER AppAdminUser;
                ALTER ROLE db_datareader DROP MEMBER AppViewerUser;
                DROP USER IF EXISTS AppAdminUser;
                DROP USER IF EXISTS AppViewerUser;
            ");

            migrationBuilder.Sql("DROP LOGIN AppAdminLogin", suppressTransaction: true);
            migrationBuilder.Sql("DROP LOGIN AppViewerLogin", suppressTransaction: true);
        }
    }
}
