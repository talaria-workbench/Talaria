using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using System.ComponentModel;
using System.Drawing;

namespace Talaria.AddIn.Image;

//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.Image.ImportImage))]
//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.CreateItemBase))]
public partial class ImportImage : CreateItemBase
{
    public override string Label => "Import Image";

    public override ImageSource Icon { get; }

    public ImportImage()
    {
        var svg = this.LoadSVG("Talaria.AddIn.Image.Resources.image_white_24dp.svg");
        this.Icon = svg;
    }


    protected override CreateItemOptions CreateDefaultCreateItemOptions(IEditorContext context) => new ImageOptions(context);


    public async Task Execute(ImageOptions options)
    {
        var context = options.Context;
        var config = options.GetConfiguration();
        var filename = config.filename;
        var source = config.importImage;

        var filePath = filename + source.Extension;
        using var destinationStream = await context.CreateFileStream(filePath);
        using var sourceStream = source.OpenRead();
        await sourceStream.CopyToAsync(destinationStream);
    }

    public override Task Execute(CreateItemOptions options) => this.Execute((ImageOptions) options);
}

public partial class ImageOptions : CreateItemOptions
{
    public ImageOptions(IEditorContext context) : base(context)
    {
        this.filenameOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.importImageOption.PropertyChanged += (_, _) => this.UpdateIsValid();
    }

    private string? Filename => this.filenameOption.Value;
    private FileInfo? ImportImage => this.importImageOption.Value;

    protected override bool AdditionalValidationSuccessfull
    {
        get
        {
            if (this.Filename is null || this.ImportImage is null) {
                return false;
            } else if (this.Filename.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) {
                return false;
            } else if (this.Context.ExistsFileOrDirectory(this.Filename + this.ImportImage.Extension)) {
                return false;
            }

            return base.AdditionalValidationSuccessfull;
        }
    }

    protected override BaseOption[] DefaultOptions() => new BaseOption[]{
            new TextOption("Filename"),
            new FileOption("Import Image", ".png")
        };
}

public class ImageEditor : IEditor
{
    public ImageEditor(IDataReference stream, IImageDecoder decoder)
    {
        this.Data = stream;
        this.Decoder = decoder;
    }

    public string Title => "Image";

    public FrameworkElement Editor => new ImageControl(this);

    public bool IsDirty => false;

    public IDataReference Data { get; }
    public IImageDecoder Decoder { get; }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged { add { } remove { } }

    public Task SaveChanges() => Task.CompletedTask;
}

public interface IImageDecoder
{
    Task<ImageSource> GenerateImage(IDataReference reference);
    public Task<Size> GetSize(IDataReference reference);
}
internal class BitmapDecocder : IImageDecoder
{
    private static IImageDecoder? instance;

    public static IImageDecoder Instance { get { return instance ??= new BitmapDecocder(); } }

    private BitmapDecocder()
    {

    }

    public async Task<Size> GetSize(IDataReference reference)
    {
        using var stream = await reference.OpenData();
        var image = System.Drawing.Image.FromStream(stream);
        return image.Size;
    }

    public async Task<ImageSource> GenerateImage(IDataReference reference)
    {
        var bit = new BitmapImage();
        using var stream = await reference.OpenData();
        await bit.SetSourceAsync(stream.AsRandomAccessStream());
        return bit;
    }
}

public class ImageInstance : InstanceBase<ImageComponent, ImageInstance>
{
    private readonly IDataReference stream;
    private readonly IImageDecoder decoder;

    public int Width { get; }
    public int Height { get; }
    protected ImageInstance(IDataReference stream, IImageDecoder decoder, int width, int height)
    {
        this.stream = stream;
        this.decoder = decoder;
        this.Width = width;
        this.Height = height;
    }

    public override IEditor CreateEditor() => new ImageEditor(this.stream, this.decoder);

    public static async Task<ImageInstance> Create(IDataReference stream, IImageDecoder decoder)
    {
        var size = await decoder.GetSize(stream);
        return new ImageInstance(stream, decoder, size.Width, size.Height);
    }
}



//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.Image.ImageComponent))]
//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.ComponentBase<Talaria.AddIn.Image.ImageComponent, Talaria.AddIn.Image.ImageInstance>))]
//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.ComponentBase))]
public partial class ImageComponent : ComponentBase<ImageComponent, ImageInstance>
{

}

//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.Image.ImageLoader))]
//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.ComponentLoaderBase<Talaria.AddIn.Image.ImageComponent, Talaria.AddIn.Image.ImageInstance>))]
//[System.ComponentModel.Composition.Export(typeof(Talaria.AddIn.ComponentLoaderBase))]
public partial class ImageLoader : ComponentLoaderBase<ImageComponent, ImageInstance>
{
    private static readonly string[] fileEndings = new string[] { ".png" };
    public override ReadOnlySpan<string> FileEndings => fileEndings.AsSpan();
    public override async Task<InstanceBase<ImageComponent, ImageInstance>> Load(IDataReference stream) => await ImageInstance.Create(stream, BitmapDecocder.Instance);
}