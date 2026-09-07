using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BodegaLuchito.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CodigoBarras = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    PrecioVenta = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnidadVenta = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ControlaInventario = table.Column<bool>(type: "INTEGER", nullable: false),
                    StockActual = table.Column<decimal>(type: "TEXT", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "TEXT", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos",
                column: "CodigoBarras",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Productos");
        }
    }
}
