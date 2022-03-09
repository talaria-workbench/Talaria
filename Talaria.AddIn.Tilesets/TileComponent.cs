namespace Talaria.AddIn.Tilesets;

public partial class TileComponent : ComponentBase<TileComponent, TileInstance>
{

}

public abstract class TileInstance : InstanceBase<TileComponent, TileInstance>
{
    public abstract int Width { get; }
    public abstract int Height { get; }
    public abstract System.Drawing.Bitmap Image { get; }

}
