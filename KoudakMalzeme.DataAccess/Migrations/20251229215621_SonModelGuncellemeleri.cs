using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoudakMalzeme.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SonModelGuncellemeleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OkulNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfilResmiYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    IlkGirisYapildiMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Malzemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GorselYolu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToplamStok = table.Column<int>(type: "int", nullable: false),
                    GuncelStok = table.Column<int>(type: "int", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Malzemeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Emanetler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UyeId = table.Column<int>(type: "int", nullable: false),
                    VerenPersonelId = table.Column<int>(type: "int", nullable: true),
                    AlanPersonelId = table.Column<int>(type: "int", nullable: true),
                    TeslimAlmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IadeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlanlananIadeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MalzemeciNotu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emanetler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Emanetler_Kullanicilar_AlanPersonelId",
                        column: x => x.AlanPersonelId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Emanetler_Kullanicilar_UyeId",
                        column: x => x.UyeId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Emanetler_Kullanicilar_VerenPersonelId",
                        column: x => x.VerenPersonelId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmanetDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmanetId = table.Column<int>(type: "int", nullable: false),
                    MalzemeId = table.Column<int>(type: "int", nullable: false),
                    AlinanAdet = table.Column<int>(type: "int", nullable: false),
                    IadeEdilenAdet = table.Column<int>(type: "int", nullable: false),
                    OlusturulmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmanetDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmanetDetaylari_Emanetler_EmanetId",
                        column: x => x.EmanetId,
                        principalTable: "Emanetler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmanetDetaylari_Malzemeler_MalzemeId",
                        column: x => x.MalzemeId,
                        principalTable: "Malzemeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmanetDetaylari_EmanetId",
                table: "EmanetDetaylari",
                column: "EmanetId");

            migrationBuilder.CreateIndex(
                name: "IX_EmanetDetaylari_MalzemeId",
                table: "EmanetDetaylari",
                column: "MalzemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Emanetler_AlanPersonelId",
                table: "Emanetler",
                column: "AlanPersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_Emanetler_UyeId",
                table: "Emanetler",
                column: "UyeId");

            migrationBuilder.CreateIndex(
                name: "IX_Emanetler_VerenPersonelId",
                table: "Emanetler",
                column: "VerenPersonelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmanetDetaylari");

            migrationBuilder.DropTable(
                name: "Emanetler");

            migrationBuilder.DropTable(
                name: "Malzemeler");

            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
