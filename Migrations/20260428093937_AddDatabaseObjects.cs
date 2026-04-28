using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. UDFs
                CREATE FUNCTION FN_PersonelMusaitMi (@PersonelId INT, @Tarih DATE)
                RETURNS BIT
                AS
                BEGIN
                    DECLARE @Musait BIT = 1;
                    IF EXISTS (SELECT 1 FROM PersonelDurumlar WHERE PersonelId = @PersonelId AND CAST(Tarih AS DATE) = @Tarih AND Uygun = 0)
                        SET @Musait = 0;
                    RETURN @Musait;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION FN_DersCakismasiVarMi (@DersId INT, @OturumId INT, @Tarih DATE)
                RETURNS BIT
                AS
                BEGIN
                    DECLARE @Cakisiyor BIT = 0;
                    DECLARE @Yariyil INT;
                    SELECT @Yariyil = Yariyil FROM Dersler WHERE Id = @DersId;

                    IF EXISTS (
                        SELECT 1 FROM Sinavlar s
                        JOIN Dersler d ON s.DersId = d.Id
                        WHERE d.Yariyil = @Yariyil AND s.OturumId = @OturumId AND CAST(s.Tarih AS DATE) = @Tarih AND s.DersId != @DersId
                    )
                    SET @Cakisiyor = 1;
                    RETURN @Cakisiyor;
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION FN_SinavToplamKapasite (@SinavId INT)
                RETURNS INT
                AS
                BEGIN
                    DECLARE @Toplam INT = 0;
                    SELECT @Toplam = ISNULL(SUM(d.Kapasite), 0)
                    FROM SinavSalonlari ss
                    JOIN Derslikler d ON ss.DerslikId = d.Id
                    WHERE ss.SinavId = @SinavId;
                    RETURN @Toplam;
                END;
            ");

            migrationBuilder.Sql(@"
                -- 2. Views
                CREATE VIEW VW_SinavProgrami AS
                SELECT s.Id AS SinavId, d.DersKodu, d.Ad AS DersAdi, d.OgrenciSayisi, d.Yariyil,
                       o.Tanim AS OturumTanim, o.BaslangicSaat, o.BitisSaat, s.Tarih
                FROM Sinavlar s
                JOIN Dersler d ON s.DersId = d.Id
                JOIN Oturumlar o ON s.OturumId = o.Id;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW VW_GozetmenGorevOzeti AS
                SELECT p.Id, p.Ad, p.Soyad, b.Ad AS Bolum, COUNT(ga.Id) AS ToplamGorev
                FROM Personeller p
                JOIN Bolumler b ON p.BolumId = b.Id
                LEFT JOIN GozetmenAtamalari ga ON p.Id = ga.PersonelId
                GROUP BY p.Id, p.Ad, p.Soyad, b.Ad;
            ");

            migrationBuilder.Sql(@"
                CREATE VIEW VW_SinifDoluluk AS
                SELECT d.Id, d.Ad, d.Kapasite, s.Tarih, o.BaslangicSaat, o.BitisSaat
                FROM Derslikler d
                JOIN SinavSalonlari ss ON d.Id = ss.DerslikId
                JOIN Sinavlar s ON ss.SinavId = s.Id
                JOIN Oturumlar o ON s.OturumId = o.Id;
            ");

            migrationBuilder.Sql(@"
                -- 3. Stored Procedures
                CREATE PROCEDURE SP_AkilliSalonHesapla
                    @SinavId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    -- Basit bir atama algoritması: Kapasitesi en büyük sınıflardan başlayarak atama yapar.
                    DECLARE @OgrenciSayisi INT;
                    SELECT @OgrenciSayisi = d.OgrenciSayisi FROM Sinavlar s JOIN Dersler d ON s.DersId = d.Id WHERE s.Id = @SinavId;
                    
                    DECLARE @AtananKapasite INT = dbo.FN_SinavToplamKapasite(@SinavId);
                    
                    IF @AtananKapasite >= @OgrenciSayisi
                    BEGIN
                        PRINT 'Yeterli kapasite zaten atanmış.';
                        RETURN;
                    END

                    -- Kapasitesi büyükten küçüğe boş derslikleri bul
                    DECLARE cur CURSOR FOR 
                    SELECT Id, Kapasite FROM Derslikler d
                    WHERE Aktif = 1 AND NOT EXISTS (
                        -- Aynı oturumda ve tarihte başka bir sınava atanmamış olacak
                        SELECT 1 FROM SinavSalonlari ss2 
                        JOIN Sinavlar s2 ON ss2.SinavId = s2.Id
                        WHERE ss2.DerslikId = d.Id AND s2.OturumId = (SELECT OturumId FROM Sinavlar WHERE Id = @SinavId)
                        AND CAST(s2.Tarih AS DATE) = (SELECT CAST(Tarih AS DATE) FROM Sinavlar WHERE Id = @SinavId)
                    )
                    ORDER BY Kapasite DESC;

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
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE SP_GozetmenHavuzAtama
                    @SinavSalonuId INT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    -- Görevi en az olan uygun personeli bul
                    DECLARE @SinavId INT, @Tarih DATE, @OturumId INT;
                    SELECT @SinavId = s.Id, @Tarih = CAST(s.Tarih AS DATE), @OturumId = s.OturumId 
                    FROM SinavSalonlari ss JOIN Sinavlar s ON ss.SinavId = s.Id WHERE ss.Id = @SinavSalonuId;

                    DECLARE @PersonelId INT;
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
                    ORDER BY (SELECT COUNT(*) FROM GozetmenAtamalari WHERE PersonelId = p.Id) ASC;

                    IF @PersonelId IS NOT NULL
                    BEGIN
                        INSERT INTO GozetmenAtamalari (SinavSalonuId, PersonelId) VALUES (@SinavSalonuId, @PersonelId);
                    END
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE SP_YedekAl
                    @Path NVARCHAR(255)
                AS
                BEGIN
                    DECLARE @DBName NVARCHAR(100) = DB_NAME();
                    DECLARE @BackupFile NVARCHAR(500) = @Path + '\' + @DBName + '_' + REPLACE(CONVERT(NVARCHAR, GETDATE(), 120), ':', '-') + '.bak';
                    BACKUP DATABASE @DBName TO DISK = @BackupFile;
                END;
            ");

            migrationBuilder.Sql(@"
                -- 4. Triggers
                CREATE TRIGGER TR_SinavLog_InsertUpdateDelete
                ON Sinavlar
                AFTER INSERT, UPDATE, DELETE
                AS
                BEGIN
                    DECLARE @IslemTipi NVARCHAR(50);
                    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
                        SET @IslemTipi = 'UPDATE';
                    ELSE IF EXISTS (SELECT * FROM inserted)
                        SET @IslemTipi = 'INSERT';
                    ELSE
                        SET @IslemTipi = 'DELETE';

                    IF @IslemTipi = 'UPDATE'
                    BEGIN
                        INSERT INTO IslemLoglari (IslemTipi, TabloAdi, KayitId, EskiDeger, YeniDeger, YapanKullanici, IslemTarihi)
                        SELECT 'UPDATE', 'Sinavlar', i.Id, 
                               'OturumId: ' + CAST(d.OturumId AS NVARCHAR), 
                               'OturumId: ' + CAST(i.OturumId AS NVARCHAR), 
                               SYSTEM_USER, GETDATE()
                        FROM inserted i JOIN deleted d ON i.Id = d.Id;
                    END
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER TR_SinavSalonuKapasiteKontrol
                ON SinavSalonlari
                AFTER INSERT
                AS
                BEGIN
                    DECLARE @SinavId INT;
                    SELECT TOP 1 @SinavId = SinavId FROM inserted;
                    
                    DECLARE @OgrenciSayisi INT;
                    SELECT @OgrenciSayisi = d.OgrenciSayisi FROM Sinavlar s JOIN Dersler d ON s.DersId = d.Id WHERE s.Id = @SinavId;
                    
                    DECLARE @ToplamKapasite INT = dbo.FN_SinavToplamKapasite(@SinavId);
                    
                    IF @ToplamKapasite > @OgrenciSayisi * 1.5
                    BEGIN
                        PRINT 'Kapasite fazlası atama yapıldı.'
                    END
                END;
            ");

            migrationBuilder.Sql(@"
                -- 5. Role-Based Security
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'App_Admin')
                BEGIN
                    CREATE ROLE App_Admin;
                END
                
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'App_Viewer')
                BEGIN
                    CREATE ROLE App_Viewer;
                END

                -- Yetkiler
                ALTER ROLE db_owner ADD MEMBER App_Admin;
                ALTER ROLE db_datareader ADD MEMBER App_Viewer;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SP_YedekAl");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SP_GozetmenHavuzAtama");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SP_AkilliSalonHesapla");
            migrationBuilder.Sql("DROP VIEW IF EXISTS VW_SinifDoluluk");
            migrationBuilder.Sql("DROP VIEW IF EXISTS VW_GozetmenGorevOzeti");
            migrationBuilder.Sql("DROP VIEW IF EXISTS VW_SinavProgrami");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS FN_SinavToplamKapasite");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS FN_DersCakismasiVarMi");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS FN_PersonelMusaitMi");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_SinavLog_InsertUpdateDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_SinavSalonuKapasiteKontrol");
            migrationBuilder.Sql("DROP ROLE IF EXISTS App_Admin");
            migrationBuilder.Sql("DROP ROLE IF EXISTS App_Viewer");
        }
    }
}
