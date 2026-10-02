using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Ventas.Views;

public partial class TicketVentaWindow : Window
{
    public TicketVentaWindow(
        int ventaId,
        string nombreCajero,
        MetodoPago metodoPago,
        decimal subtotal,
        decimal igv,
        decimal total,
        IEnumerable<DetalleCarritoVenta> productos)
    {
        InitializeComponent();
        CargarDatosReales(ventaId, nombreCajero, metodoPago, subtotal, igv, total, productos);
    }

    private void CargarDatosReales(
        int ventaId,
        string nombreCajero,
        MetodoPago metodoPago,
        decimal subtotal,
        decimal igv,
        decimal total,
        IEnumerable<DetalleCarritoVenta> productos)
    {
        TxtTicketId.Text = ventaId.ToString("D6");
        TxtFecha.Text = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");
        TxtCajero.Text = nombreCajero;
        TxtMetodoPago.Text = metodoPago.ToString();

        ListaProductos.ItemsSource = productos.Select(p => new
        {
            Cantidad = p.Cantidad,
            NombreProducto = p.Nombre,
            Subtotal = p.Subtotal
        }).ToList();

        TxtSubtotal.Text = $"S/ {subtotal:N2}";
        TxtIgv.Text = $"S/ {igv:N2}";
        TxtTotal.Text = $"S/ {total:N2}";
    }

    // Permite mover la ventana arrastrando la nueva cabecera gris
    private void BarraSuperior_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void BtnCerrar_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                // Como los botones ya no están dentro de TicketContenedor, simplemente mandamos a imprimir
                double anchoSeguro = 250;

                TicketContenedor.Width = anchoSeguro;
                TicketContenedor.Margin = new Thickness(0);

                TicketContenedor.Measure(new Size(anchoSeguro, double.PositiveInfinity));
                TicketContenedor.Arrange(new Rect(new Point(0, 0), TicketContenedor.DesiredSize));

                printDialog.PrintVisual(TicketContenedor, "Bodega Luchito - Ticket");
                MessageBox.Show("Ticket enviado a la impresora.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al intentar imprimir: {ex.Message}", "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
