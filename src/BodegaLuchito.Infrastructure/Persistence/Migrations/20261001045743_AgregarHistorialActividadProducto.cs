using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BodegaLuchito.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarHistorialActividadProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialActividadProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioNombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialActividadProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialActividadProducto_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialActividadProducto_ProductoId_FechaHora",
                table: "HistorialActividadProducto",
                columns: new[] { "ProductoId", "FechaHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialActividadProducto");
        }
    }
}
