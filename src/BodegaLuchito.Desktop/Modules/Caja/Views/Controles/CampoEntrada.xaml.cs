
using System.Windows;
using System.Windows.Controls;
namespace BodegaLuchito.Desktop.Modules.Caja.Views.Controles
{
    public partial class CampoEntrada : UserControl
    {
        public CampoEntrada()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty EtiquetaProperty =
            DependencyProperty.Register(
                nameof(Etiqueta),
                typeof(string),
                typeof(CampoEntrada),
                new PropertyMetadata(string.Empty));

        public string Etiqueta
        {
            get => (string)GetValue(EtiquetaProperty);
            set => SetValue(EtiquetaProperty, value);

        }

        public static readonly DependencyProperty ValorProperty =
        DependencyProperty.Register(
                nameof(Valor),
                typeof(string),
                typeof(CampoEntrada),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Valor
        {
            get => (string)GetValue(ValorProperty);
            set => SetValue(ValorProperty, value);
        }

    }
}
