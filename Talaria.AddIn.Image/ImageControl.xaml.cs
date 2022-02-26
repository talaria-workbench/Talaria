using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Talaria.AddIn.Image;
public sealed partial class ImageControl : UserControl
{
    public BitmapImage ImageSource { get; private set; }
    public ImageEditor ImageEditor { get; }

    public ImageControl(ImageEditor imageEditor)
    {
        this.ImageEditor = imageEditor;
        var bitmapImage = new BitmapImage();
        this.ImageSource = bitmapImage;
        this.InitializeComponent();
        this.Loaded += this.ImageControl_Loaded;
    }

    private async void ImageControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        using var stream = await this.ImageEditor.Data.OpenData();
        await this.ImageSource.SetSourceAsync(stream.AsRandomAccessStream());
    }
}
