using BodegaLuchito.Desktop.Common.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Productos.ViewModels;

public partial class ProductosViewModel : ViewModelBase
{
    public RegistrarProductoViewModel Registro { get; }

    public ProductosViewModel(RegistrarProductoViewModel registro)
    {
        Registro = registro;
    }
}
