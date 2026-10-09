using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BodegaLuchito.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResolucionDescuadreCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DescuadreResuelto",
                table: "Sesionescaja",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaResolucionDescuadre",
                table: "Sesionescaja",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionResolucionDescuadre",
                table: "Sesionescaja",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioResolucionId",
                table: "Sesionescaja",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescuadreResuelto",
                table: "Sesionescaja");

            migrationBuilder.DropColumn(
                name: "FechaResolucionDescuadre",
                table: "Sesionescaja");

            migrationBuilder.DropColumn(
                name: "ObservacionResolucionDescuadre",
                table: "Sesionescaja");

            migrationBuilder.DropColumn(
                name: "UsuarioResolucionId",
                table: "Sesionescaja");
        }
    }
}
