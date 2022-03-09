using Microsoft.UI.Xaml;

using Talaria.AddIn.Image;

namespace Talaria.AddIn.Tilesets;


public partial class ImageTilesetLoader : ComponentLoaderBase<TilesetComponent, TilesetInstance>
{
    private static string[] fileEndings = new string[] { ".imageTileset" };
    public override ReadOnlySpan<string> FileEndings => fileEndings.AsSpan();

    public override async Task<InstanceBase<TilesetComponent, TilesetInstance>> Load(IDataReference reference)
    {
        return new TilesetImageInstance(reference);
    }
}

public partial class TilesetComponent : ComponentBase<TilesetComponent, TilesetInstance>
{

}

public abstract class TilesetInstance : InstanceBase<TilesetComponent, TilesetInstance>
{
    public string Name { get; set; } = string.Empty;
    public abstract int TileWidth { get; }
    public abstract int TileHeight { get; }

    public abstract ITileCollection Tiles { get; }
}

public interface ITileCollection
{
    public TileInstance? this[int x, int y] { get; set; }
    public int Width { get; }
    public int Height { get; }
}

public class TilesetImageInstance : TilesetInstance
{
    private IDataReference reference;

    public TilesetImageInstance(IDataReference reference)
    {
        this.reference = reference;
    }

    public ImageInstance? Image { get; set; }
    public (int widht, int height) TileSize { get; set; }

    public override int TileWidth => this.TileSize.widht;

    public override int TileHeight => this.TileSize.height;

    public int HorizontalGap { get; set; }
    public int VerticalGap { get; set; }
    public int MarginTop { get; set; }
    public int MarginLeft { get; set; }
    public int MarginRight { get; set; }
    public int MarginBottom { get; set; }

    public override ITileCollection Tiles => throw new NotImplementedException();

    public override IEditor CreateEditor() => new TilesetImageEditor(this);
}

internal partial class TilesetImageEditor : IEditor
{
    private TilesetImageInstance tilesetInstance;

    public TilesetImageEditor(TilesetImageInstance tilesetInstance)
    {
        this.tilesetInstance = tilesetInstance;
        this.Editor = new TilesetEditor(this);
    }

    public string Title => this.tilesetInstance.Name;

    public FrameworkElement Editor { get; }

    [SourceGenerators.AutoNotify]
    public bool isDirty;

    public Task SaveChanges()
    {
        return Task.CompletedTask;
    }
}