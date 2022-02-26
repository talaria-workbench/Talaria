using Microsoft.UI.Xaml.Media;

using System.ComponentModel.Composition;

namespace Talaria.AddIn.Image;

[Export(typeof(CreateItemBase))]
public class ImportImage : CreateItemBase
{
    public override string Label => "Import Image";

    public override ImageSource Icon { get; }

    public ImportImage()
    {
        var svg = this.LoadSVG("Talaria.AddIn.Image.Resources.image_white_24dp.svg");
        this.Icon = svg;
    }


    protected override CreateItemOptions CreateDefaultCreateItemOptions(IEditorContext context) => new ImageOptions(context);


    public override async Task Execute(CreateItemOptions options)
    {
        var context = options.Context;
        var config = options.GetConfiguration();
        var filename = ((Configuration<string>)config[0]).Value;
        var source = ((Configuration<FileInfo>)config[1]).Value;

        var filePath = filename + source.Extension;
        using var destinationStream = await context.CreateFileStream(filePath);
        using var sourceStream = source.OpenRead();
        await sourceStream.CopyToAsync(destinationStream);
    }
}

public class ImageOptions : CreateItemOptions
{
    public ImageOptions(IEditorContext context) : base(context)
    {
        this.FilenameOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.ImportImageOption.PropertyChanged += (_, _) => this.UpdateIsValid();
    }

    private string? Filename => ((TextOption) this.Options[0]).Value;
    private FileInfo? ImportImage => ((FileOption) this.Options[1]).Value;
    private TextOption FilenameOption => ((TextOption) this.Options[0]);
    private FileOption ImportImageOption => ((FileOption) this.Options[1]);

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
