using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamScheduler.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpAkilliAtamaOverlapCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

                    DECLARE cur CURSOR FOR 
                    SELECT Id, Kapasite FROM Derslikler d
                    WHERE Aktif = 1 AND NOT EXISTS (
                        SELECT 1 FROM SinavSalonlari ss2 
                        JOIN Sinavlar s2 ON ss2.SinavId = s2.Id
                        JOIN Oturumlar o2 ON s2.OturumId = o2.Id
                        WHERE ss2.DerslikId = d.Id AND CAST(s2.Tarih AS DATE) = @TargetTarih
                        AND o2.BaslangicSaat < @TargetBitis
                        AND o2.BitisSaat > @TargetBaslangic
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
