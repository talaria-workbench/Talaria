using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using System.ComponentModel.Composition;

namespace Talaria.AddIn.Image
{
    [Export(typeof(ICreateItem))]
    public class ImportImage : ICreateItem
    {
        public string Label => "Import Image";

        public ImageSource Icon { get; }

        public ImportImage()
        {
            var svg = new SvgImageSource();
            setImage(svg);
            this.Icon = svg;
            static async void setImage(SvgImageSource svg)
            {
                try {
                    svg.RasterizePixelHeight = 24;
                    svg.RasterizePixelWidth = 24;
                    using var stream = typeof(ImportImage).Assembly.GetManifestResourceStream("Talaria.AddIn.Image.Resources.image_white_24dp.svg");
                    var status = await svg.SetSourceAsync(stream.AsRandomAccessStream());
                    
                } catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine($"Faild to load Image {e}");

                }
            }
        }

        public CreateItemOptions DefaultCreateItemOptions { get; } = new ImageOptions();

        public Task Execute(IEditorContext context, CreateItemOptions options)
        {
            var config = options.GetConfiguration();
            return Task.CompletedTask;
        }
    }

    public class ImageOptions : CreateItemOptions
    {

        protected override BaseOption[] DefaultOptions() => new BaseOption[]{
            new TextOption("Filename"),
            new FileOption("Import Image", ".png")
        };
    }
}
