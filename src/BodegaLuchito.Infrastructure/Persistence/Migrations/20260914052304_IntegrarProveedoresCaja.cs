using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BodegaLuchito.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrarProveedoresCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proveedor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Ruc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sesionescaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UsuarioAperturaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FondoInicial = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioCierreId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EfectivoEsperado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EfectivoReal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiferenciaEfectivo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    YapeEsperado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    YapeReal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiferenciaYape = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PlinEsperado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PlinReal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiferenciaPlin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ObservacionCierre = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesionescaja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SesionCajaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    MetodoPago = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: true),
                    AbastecimientoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosCaja_Sesionescaja_SesionCajaId",
                        column: x => x.SesionCajaId,
                        principalTable: "Sesionescaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_SesionCajaId",
                table: "MovimientosCaja",
                column: "SesionCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedor_Ruc",
                table: "Proveedor",
                column: "Ruc",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosCaja");

            migrationBuilder.DropTable(
                name: "Proveedor");

            migrationBuilder.DropTable(
                name: "Sesionescaja");
        }
    }
}
