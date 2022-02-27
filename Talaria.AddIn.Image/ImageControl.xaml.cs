using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Talaria.AddIn.Image;
public sealed partial class ImageControl : UserControl
{
    //public BitmapImage ImageSource { get; private set; }



    public ImageSource ImageSource
    {
        get { return (ImageSource) this.GetValue(ImageSourceProperty); }
        set { this.SetValue(ImageSourceProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageControl), new PropertyMetadata(null));



    public ImageEditor ImageEditor { get; }

    public ImageControl(ImageEditor imageEditor)
    {
        this.ImageEditor = imageEditor;
        this.InitializeComponent();
        this.Loaded += this.ImageControl_Loaded;
    }

    private async void ImageControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var source = await this.ImageEditor.Decoder.GenerateImage(this.ImageEditor.Data);
        this.ImageSource = source;
    }
}
