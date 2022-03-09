
using Microsoft.UI.Xaml.Media;

using System.Text;

using Talaria.AddIn.Image;

namespace Talaria.AddIn.Tilesets;

public partial class CreateImageTileset : CreateItemBase
{
    public override string Label => "Create Tileset from Image";

    public override ImageSource Icon { get; }

    public CreateImageTileset()
    {
        var svg = this.LoadSVG("Talaria.AddIn.Image.Resources.apps_white_24dp.svg");
        this.Icon = svg;
    }


    protected override CreateItemOptions CreateDefaultCreateItemOptions(IEditorContext context) => new ImageTilesetOptions(context);


    public async Task Execute(ImageTilesetOptions options)
    {
        var context = options.Context;
        var config = options.GetConfiguration();
        var filename = config.tilesetName + ".imageTileset";
        //var source = ((Configuration<FileInfo>) config[1]).Value;

        using var destinationStream = await context.CreateFileStream(filename);
        await destinationStream.WriteAsync(Encoding.UTF8.GetBytes("<tileset />"));
    }
    public override Task Execute(CreateItemOptions options)
    {
        return this.Execute((ImageTilesetOptions) options);
    }
}

public partial class ImageTilesetOptions : CreateItemOptions
{
    public ImageTilesetOptions(IEditorContext context) : base(context)
    {
        this.tilesetNameOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.imageOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.tileWidthOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.tileHeightOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.horizontalGapOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.verticalGapOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.marginBottomOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.marginTopOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.marginLeftOption.PropertyChanged += (_, _) => this.UpdateIsValid();
        this.marginRightOption.PropertyChanged += (_, _) => this.UpdateIsValid();

    }


    protected override bool AdditionalValidationSuccessfull
    {
        get
        {
            if (this.tilesetNameOption.Value is null || this.imageOption.Value is null) {
                return false;
            } else if (this.tilesetNameOption.Value.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) {
                return false;
            } else if (this.Context.ExistsFileOrDirectory(this.tilesetNameOption.Value + ".imageTileset")) {
                return false;
            }
            var tileWidth = this.tileWidthOption.Value;
            var tileHeight = this.tileHeightOption.Value;
            var imagewidth = this.imageOption.Value.Width;

            var imageheight = this.imageOption.Value.Height;

            imagewidth -= this.marginRightOption.Value + this.marginLeftOption.Value;
            var tilesX = imagewidth / (tileWidth + this.horizontalGapOption.Value);
            var allTileWidth = tilesX * (tileWidth + this.horizontalGapOption.Value);
            if (imagewidth - allTileWidth != this.horizontalGapOption.Value) {
                return false;
            }
            imageheight -= this.marginTopOption.Value + this.marginBottomOption.Value;
            var tilesY = imageheight / (tileHeight + this.verticalGapOption.Value);
            var allTileHeight = tilesY * (tileHeight + this.verticalGapOption.Value);
            if (imageheight - allTileHeight != this.verticalGapOption.Value) {
                return false;
            }


            return base.AdditionalValidationSuccessfull;
        }
    }

    protected override BaseOption[] DefaultOptions() => new BaseOption[]{
            new TextOption("Tileset Name"),
            new ReferenceInstanceOption<ImageComponent,ImageInstance>("Image"),
            new IntOption("Tile Width"),
            new IntOption("Tile Height"),
            new IntOption("Horizontal Gap"),
            new IntOption("Vertical Gap"),
            new IntOption("Margin Left"),
            new IntOption("Margin Right"),
            new IntOption("Margin Top"),
            new IntOption("Margin Bottom"),
        };
}

