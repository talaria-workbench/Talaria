using System.Collections.ObjectModel;

namespace Talaria.AddIn;

public abstract class ComponentInstanceBase
{
    private protected ComponentInstanceBase()
    {

    }
}


public abstract class ComponentInstanceBase<TComponent> : ComponentInstanceBase
    where TComponent : ComponentBase<TComponent>
{
    public ObservableCollection<IMetadataComponent<TComponent>> Metadata { get; } = new();

}
