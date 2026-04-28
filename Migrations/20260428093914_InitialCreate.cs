using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamScheduler.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bolumler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolumler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Derslikler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Kapasite = table.Column<int>(type: "int", nullable: false),
                    Tip = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Aktif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Derslikler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IslemLoglari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IslemTipi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TabloAdi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    KayitId = table.Column<int>(type: "int", nullable: false),
                    EskiDeger = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YeniDeger = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YapanKullanici = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IslemLoglari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Oturumlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tanim = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaslangicSaat = table.Column<TimeSpan>(type: "time", nullable: false),
                    BitisSaat = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oturumlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dersler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DersKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DersTuru = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OgrenciSayisi = table.Column<int>(type: "int", nullable: false),
                    Yariyil = table.Column<int>(type: "int", nullable: false),
                    BolumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dersler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dersler_Bolumler_BolumId",
                        column: x => x.BolumId,
                        principalTable: "Bolumler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Personeller",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unvan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BolumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personeller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personeller_Bolumler_BolumId",
                        column: x => x.BolumId,
                        principalTable: "Bolumler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sinavlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DersId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OturumId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sinavlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sinavlar_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sinavlar_Oturumlar_OturumId",
                        column: x => x.OturumId,
                        principalTable: "Oturumlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonelDurumlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonelId = table.Column<int>(type: "int", nullable: false),
                    Tarih = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MazeretTuru = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Uygun = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonelDurumlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonelDurumlar_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SinavSalonlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinavId = table.Column<int>(type: "int", nullable: false),
                    DerslikId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinavSalonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinavSalonlari_Derslikler_DerslikId",
                        column: x => x.DerslikId,
                        principalTable: "Derslikler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinavSalonlari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GozetmenAtamalari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SinavSalonuId = table.Column<int>(type: "int", nullable: false),
                    PersonelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GozetmenAtamalari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GozetmenAtamalari_Personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "Personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GozetmenAtamalari_SinavSalonlari_SinavSalonuId",
                        column: x => x.SinavSalonuId,
                        principalTable: "SinavSalonlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_BolumId",
                table: "Dersler",
                column: "BolumId");

            migrationBuilder.CreateIndex(
                name: "IX_GozetmenAtamalari_PersonelId",
                table: "GozetmenAtamalari",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_GozetmenAtamalari_SinavSalonuId",
                table: "GozetmenAtamalari",
                column: "SinavSalonuId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonelDurumlar_PersonelId",
                table: "PersonelDurumlar",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Personeller_BolumId",
                table: "Personeller",
                column: "BolumId");

            migrationBuilder.CreateIndex(
                name: "IX_Sinavlar_DersId",
                table: "Sinavlar",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_Sinavlar_OturumId",
                table: "Sinavlar",
                column: "OturumId");

            migrationBuilder.CreateIndex(
                name: "IX_SinavSalonlari_DerslikId",
                table: "SinavSalonlari",
                column: "DerslikId");

            migrationBuilder.CreateIndex(
                name: "IX_SinavSalonlari_SinavId",
                table: "SinavSalonlari",
                column: "SinavId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GozetmenAtamalari");

            migrationBuilder.DropTable(
                name: "IslemLoglari");

            migrationBuilder.DropTable(
                name: "PersonelDurumlar");

            migrationBuilder.DropTable(
                name: "SinavSalonlari");

            migrationBuilder.DropTable(
                name: "Personeller");

            migrationBuilder.DropTable(
                name: "Derslikler");

            migrationBuilder.DropTable(
                name: "Sinavlar");

            migrationBuilder.DropTable(
                name: "Dersler");

            migrationBuilder.DropTable(
                name: "Oturumlar");

            migrationBuilder.DropTable(
                name: "Bolumler");
        }
    }
}
