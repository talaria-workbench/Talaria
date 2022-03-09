using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Talaria.AddIn.Tilesets;
public sealed partial class TilesetEditor : UserControl
{
    private readonly TilesetImageEditor tilesetImageEditor;

    internal TilesetEditor(TilesetImageEditor tilesetImageEditor)
    {
        this.InitializeComponent();
        this.tilesetImageEditor = tilesetImageEditor;
    }
}
