using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoudakMalzeme.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EtiketAlaniEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Etiketler",
                table: "Malzemeler",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Etiketler",
                table: "Malzemeler");
        }
    }
}
