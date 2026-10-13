using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace BodegaLuchito.Application.Reportes.Servicios;

public class ExportacionExcelService
{
    public string ExportarAExcel<T>(IEnumerable<T> datos, string tituloReporte, string rutaCarpeta)
    {
        if (datos == null || !datos.Any())
            throw new InvalidOperationException("No hay datos para exportar.");

        var primerElemento = datos.FirstOrDefault(x => x != null);
        if (primerElemento == null)
            throw new InvalidOperationException("Los datos proporcionados son nulos o inválidos.");

        var propiedades = primerElemento.GetType().GetProperties();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(tituloReporte);

        // 1. Título General
        worksheet.Cell(1, 1).Value = $"Reporte: {tituloReporte}";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        worksheet.Range(1, 1, 1, propiedades.Length > 0 ? propiedades.Length : 5).Merge();

        worksheet.Cell(2, 1).Value = $"Generado el: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;

        int colIndex = 1;
        int rowIndex = 4;

        // 2. Escribir Cabeceras
        foreach (var prop in propiedades)
        {
            var cell = worksheet.Cell(rowIndex, colIndex);
            cell.Value = prop.Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            colIndex++;
        }

        // 3. Llenar los datos
        rowIndex++;
        foreach (var item in datos)
        {
            if (item == null) continue;

            colIndex = 1;
            foreach (var prop in propiedades)
            {
                var valor = prop.GetValue(item);
                var cell = worksheet.Cell(rowIndex, colIndex);

                // CORRECCIÓN: Formatos seguros para Excel
                if (valor is decimal decValor)
                {
                    cell.Value = decValor;
                    // Usamos un formato numérico contable estándar y seguro en lugar del símbolo explícito "S/"
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (valor is DateTime dateValor)
                {
                    cell.Value = dateValor;
                    // Formato de fecha seguro
                    cell.Style.NumberFormat.Format = "dd/MM/yyyy hh:mm AM/PM";
                }
                else
                {
                    cell.Value = valor?.ToString() ?? "";
                }

                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                colIndex++;
            }
            rowIndex++;
        }

        // 4. Autoajustar el tamaño de las columnas
        worksheet.Columns().AdjustToContents();

        // 5. Guardar el archivo
        string nombreArchivo = $"{tituloReporte.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        string rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

        if (!Directory.Exists(rutaCarpeta))
            Directory.CreateDirectory(rutaCarpeta);

        workbook.SaveAs(rutaCompleta);

        return rutaCompleta;
    }
}
