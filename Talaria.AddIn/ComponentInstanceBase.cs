namespace Talaria.AddIn;

public abstract class ComponentInstanceBase
{
    private protected ComponentInstanceBase()
    {

    }

    public abstract IEditor CreateEditor();

}


public abstract class ComponentInstanceBase<TComponent, TInstance> : ComponentInstanceBase
    where TComponent : ComponentBase<TComponent, TInstance>
    where TInstance : ComponentInstanceBase<TComponent, TInstance>
{
    // TODO: Reactiate Subcomponents
    //public ObservableCollection<IMetadataComponent<TComponent>> Metadata { get; } = new();

}
