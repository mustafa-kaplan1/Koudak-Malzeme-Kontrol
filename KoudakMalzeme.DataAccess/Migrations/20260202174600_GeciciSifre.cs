using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoudakMalzeme.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class GeciciSifre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeçiciŞifre",
                table: "Kullanicilar",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeçiciŞifre",
                table: "Kullanicilar");
        }
    }
}
