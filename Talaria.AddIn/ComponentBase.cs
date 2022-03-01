using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Xml;

namespace Talaria.AddIn;


public abstract class ComponentLoaderBase
{
    private protected ComponentLoaderBase()
    {

    }
    public virtual ReadOnlySpan<string> FileEndings => ReadOnlySpan<string>.Empty;

    public virtual Task<bool> CanLoad(IDataReference reference)
    {

        return reference is IFileDataReference file
            ? Task.FromResult(this.FileEndings.Contains(Path.GetExtension(file.Name)))
            : Task.FromResult(false);

    }
    public Task<InstanceBase> Load(IDataReference reference) => this.InternalLoad(reference);
    private protected abstract Task<InstanceBase> InternalLoad(IDataReference reference);


}
public abstract class ComponentLoaderBase<TComponent, TInstance> : ComponentLoaderBase
    where TComponent : ComponentBase<TComponent, TInstance>
    where TInstance : InstanceBase<TComponent, TInstance>
{

    private protected sealed override async Task<InstanceBase> InternalLoad(IDataReference reference) => await this.Load(reference);


    public new abstract Task<InstanceBase<TComponent, TInstance>> Load(IDataReference reference);

}

public abstract class ComponentBase : IComponentBase
{
    private protected ComponentBase()
    {

    }

    public abstract Task<bool> CanLoad(IDataReference reference);

    public virtual Task<InstanceBase?> TryLoad(IDataReference reference) => this.InternalLoad(reference);
    public async Task<InstanceBase> Load(IDataReference reference) => (await this.TryLoad(reference)) ?? throw new ArgumentException($"Cant load {reference} with {this}");
    private protected abstract Task<InstanceBase?> InternalLoad(IDataReference reference);
}

public abstract class ComponentBase<TComponent, TInstance> : ComponentBase
    where TComponent : ComponentBase<TComponent, TInstance>
    where TInstance : InstanceBase<TComponent, TInstance>
{

    [ImportMany(AllowRecomposition = true, RequiredCreationPolicy = CreationPolicy.Shared, Source = ImportSource.Any)]
    private readonly ObservableCollection<ComponentLoaderBase<TComponent, TInstance>> loader = new();

    public override async Task<bool> CanLoad(IDataReference reference)
    {
        var posiibleComponents = this.loader.ToAsyncEnumerable().WhereAwait(async x => await x.CanLoad(reference));
        var component = await posiibleComponents.SingleOrDefaultAsync();
        return component != null;
    }

    private protected override async Task<InstanceBase?> InternalLoad(IDataReference reference)
    {

        var posiibleComponents = this.loader.ToAsyncEnumerable().WhereAwait(async x => await x.CanLoad(reference));
        var component = await posiibleComponents.SingleOrDefaultAsync();
        return component is not null
            ? await component.Load(reference)
            : null;
    }

}

public interface IComponentBase
{
    Task<InstanceBase?> TryLoad(IDataReference reference);
    Task<InstanceBase> Load(IDataReference reference);
    Task<bool> CanLoad(IDataReference reference);


}

public interface IDataReference
{
    Task<Stream> OpenData();
    Task SaveData(Stream stream);
    public async Task<XmlDocument> OpenXml()
    {
        using var stream = await this.OpenData();
        var doc = new XmlDocument();
        doc.Load(stream);
        return doc;
    }
    public async Task SaveXml(XmlDocument doc)
    {

        using var stream = new MemoryStream();
        doc.Save(stream);
        _ = stream.Seek(0, SeekOrigin.Begin);
        await this.SaveData(stream);
    }
}
public interface IFileDataReference : IDataReference
{
    string Name { get; }
    string LocalPath { get; }
}
