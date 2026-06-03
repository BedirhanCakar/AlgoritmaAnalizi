using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamScheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneToKullanici : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TelefonNumarasi",
                table: "Kullanicilar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE Kullanicilar 
                SET TelefonNumarasi = '5' + RIGHT('000000000' + CAST(Id AS VARCHAR(9)), 9)
                WHERE TelefonNumarasi = '' OR TelefonNumarasi IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_TelefonNumarasi",
                table: "Kullanicilar",
                column: "TelefonNumarasi",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_TelefonNumarasi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "TelefonNumarasi",
                table: "Kullanicilar");
        }
    }
}
