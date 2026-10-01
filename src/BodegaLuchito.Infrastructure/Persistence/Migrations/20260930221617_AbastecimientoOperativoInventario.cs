using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BodegaLuchito.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AbastecimientoOperativoInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioUnitario",
                table: "DetalleAbastecimiento",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    StockAnterior = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    StockPosterior = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AbastecimientoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosInventario_Producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_ProductoId_FechaHora",
                table: "MovimientosInventario",
                columns: new[] { "ProductoId", "FechaHora" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioUnitario",
                table: "DetalleAbastecimiento",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");
        }
    }
}
