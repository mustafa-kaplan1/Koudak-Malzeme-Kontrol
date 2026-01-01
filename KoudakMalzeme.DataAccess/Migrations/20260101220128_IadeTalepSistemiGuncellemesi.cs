using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoudakMalzeme.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IadeTalepSistemiGuncellemesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TeslimAlmaTarihi",
                table: "Emanetler",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IadeTalepEdilenAdet",
                table: "EmanetDetaylari",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IadeTalepEdilenAdet",
                table: "EmanetDetaylari");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TeslimAlmaTarihi",
                table: "Emanetler",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
