using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamScheduler.Migrations
{
    /// <inheritdoc />
    public partial class ChangePhoneToEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_TelefonNumarasi",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "TelefonNumarasi",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<string>(
                name: "Eposta",
                table: "Kullanicilar",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE Kullanicilar 
                SET Eposta = 'kullanici' + CAST(Id AS VARCHAR(10)) + '@ortakproje.com'
                WHERE Eposta = '' OR Eposta IS NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Eposta",
                table: "Kullanicilar",
                column: "Eposta",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Kullanicilar_Eposta",
                table: "Kullanicilar");

            migrationBuilder.DropColumn(
                name: "Eposta",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<string>(
                name: "TelefonNumarasi",
                table: "Kullanicilar",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_TelefonNumarasi",
                table: "Kullanicilar",
                column: "TelefonNumarasi",
                unique: true);
        }
    }
}
