using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;

namespace HotChocolate.Execution.Processing;

public readonly struct SelectionFeatureCollection : IFeatureCollection
{
    private readonly OperationFeatureCollection _parent;
    private readonly int _selectionId;

    internal SelectionFeatureCollection(OperationFeatureCollection parent, int selectionId)
    {
        _parent = parent;
        _selectionId = selectionId;
    }

    public bool IsReadOnly => _parent.IsReadOnly;

    public bool IsEmpty => !_parent.HasFeatures(_selectionId);

    public int Revision => _parent.Revision;

    public object? this[Type key]
    {
        get => _parent[_selectionId, key];
        set => _parent[_selectionId, key] = value;
    }

    public bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature)
        => _parent.TryGet(_selectionId, out feature);

    public TFeature GetOrSetSafe<TFeature>() where TFeature : new()
        => GetOrSetSafe<TFeature>(static () => new TFeature());

    internal TFeature GetOrSetSafe<TFeature>(Func<TFeature> factory)
        => _parent.GetOrSetSafe(_selectionId, factory);

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        => _parent.GetFeatures(_selectionId).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
