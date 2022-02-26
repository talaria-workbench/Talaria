using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Talaria;

[Serializable]
[DebuggerDisplay("Count = {Count}")]
public class TransformObservableCollection<TSource, TTransformed> : IReadOnlyList<TTransformed>, INotifyCollectionChanged, INotifyPropertyChanged
    where TTransformed : class
    where TSource : class
{
    private readonly ConditionalWeakTable<TSource, TTransformed> table = new();
    private readonly ObservableCollection<TSource> list;
    private readonly Func<TSource, TTransformed> transform;

    public int Count => this.list.Count;

    public TTransformed this[int index]
    {
        get {
            var original = this.list[index];
            var transformed = this.table.GetValue(original, x => this.transform(x));
            return transformed;
        }
    }

    /// <summary>
    /// Initializes a new instance of ReadOnlyObservableCollection that
    /// wraps the given ObservableCollection.
    /// </summary>
    public TransformObservableCollection(ObservableCollection<TSource> list, Func<TSource, TTransformed> transform)
    {
        this.list = list;
        this.transform = transform;
        ((INotifyCollectionChanged)list).CollectionChanged += new NotifyCollectionChangedEventHandler(this.HandleCollectionChanged);
        ((INotifyPropertyChanged)list).PropertyChanged += new PropertyChangedEventHandler(this.HandlePropertyChanged);
    }

    /// <summary>
    /// CollectionChanged event (per <see cref="INotifyCollectionChanged" />).
    /// </summary>
    event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
    {
        add => CollectionChanged += value;
        remove => CollectionChanged -= value;
    }

    /// <summary>
    /// Occurs when the collection changes, either by adding or removing an item.
    /// </summary>
    /// <remarks>
    /// see <seealso cref="INotifyCollectionChanged"/>
    /// </remarks>
    [field: NonSerialized]
    protected virtual event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// raise CollectionChanged event to any listeners
    /// </summary>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
    {
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(args.Action, args.NewItems, args.OldItems, Math.Max(args.NewStartingIndex, args.NewStartingIndex)));
    }

    /// <summary>
    /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
    /// </summary>
    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    /// <summary>
    /// Occurs when a property changes.
    /// </summary>
    /// <remarks>
    /// see <seealso cref="INotifyPropertyChanged"/>
    /// </remarks>
    [field: NonSerialized]
    protected virtual event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// raise PropertyChanged event to any listeners
    /// </summary>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }

    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.OnCollectionChanged(e);
    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        this.OnPropertyChanged(e);
    }

    public IEnumerator<TTransformed> GetEnumerator() => throw new NotImplementedException();
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
