using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Features;

internal sealed class EmptyFeatureCollection : IFeatureCollection
{
    private EmptyFeatureCollection()
    {
    }

    public bool IsReadOnly => true;

    public bool IsEmpty => true;

    public int Revision => 0;

    public object? this[Type key]
    {
        get => null;
        set => ThrowReadOnly();
    }

    /// <inheritdoc />
    public TFeature? Get<TFeature>()
        => default;

    /// <inheritdoc />
    public bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature)
    {
        feature = default;
        return false;
    }

    /// <inheritdoc />
    public void Set<TFeature>(TFeature? instance)
        => ThrowReadOnly();

    [DoesNotReturn]
    private static void ThrowReadOnly()
        => throw new NotSupportedException("The feature collection is read-only.");

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        yield break;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static EmptyFeatureCollection Default { get; } = new();
}
