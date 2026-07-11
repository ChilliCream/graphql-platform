using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// Represents a collection of features associated with a specific selection
/// within a GraphQL operation. This struct provides a view into the parent
/// <see cref="OperationFeatureCollection"/> scoped to a single selection.
/// </summary>
public readonly struct SelectionFeatureCollection : IFeatureCollection
{
    private readonly OperationFeatureCollection _parent;
    private readonly int _selectionId;

    internal SelectionFeatureCollection(OperationFeatureCollection parent, int selectionId)
    {
        _parent = parent;
        _selectionId = selectionId;
    }

    /// <summary>
    /// Gets a value indicating whether this feature collection is read-only.
    /// </summary>
    public bool IsReadOnly => _parent.IsReadOnly;

    /// <summary>
    /// Gets a value indicating whether this selection has no features.
    /// </summary>
    public bool IsEmpty => !_parent.HasFeatures(_selectionId);

    /// <summary>
    /// Gets the revision number of the underlying feature collection.
    /// </summary>
    public int Revision => _parent.Revision;

    /// <summary>
    /// Gets or sets a feature by its type.
    /// </summary>
    /// <param name="key">The type of the feature.</param>
    /// <returns>The feature instance, or <c>null</c> if not found.</returns>
    public object? this[Type key]
    {
        get => _parent[_selectionId, key];
        set => _parent[_selectionId, key] = value;
    }

    /// <summary>
    /// Gets a feature of the specified type.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <returns>The feature instance, or <c>null</c> if not found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TFeature"/> is a non-nullable value type and the feature does not exist.
    /// </exception>
    public TFeature? Get<TFeature>()
    {
        if (typeof(TFeature).IsValueType)
        {
            var feature = this[typeof(TFeature)];
            if (feature is null && Nullable.GetUnderlyingType(typeof(TFeature)) is null)
            {
                throw new InvalidOperationException(
                    $"{typeof(TFeature).FullName} does not exist in the feature collection "
                    + "and because it is a struct the method can't return null. "
                    + $"Use 'featureCollection[typeof({typeof(TFeature).FullName})] is not null' "
                    + "to check if the feature exists.");
            }

            return (TFeature?)feature;
        }

        return (TFeature?)this[typeof(TFeature)];
    }

    /// <summary>
    /// Sets a feature instance for this selection.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <param name="instance">The feature instance to set, or <c>null</c> to remove.</param>
    /// <remarks>This method is thread-safe.</remarks>
    public void SetSafe<TFeature>(TFeature? instance)
        => this[typeof(TFeature)] = instance;

    /// <summary>
    /// Tries to get a feature of the specified type.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <param name="feature">
    /// When this method returns, contains the feature if found; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the feature was found; otherwise, <c>false</c>.</returns>
    public bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature)
        => _parent.TryGet(_selectionId, out feature);

    /// <summary>
    /// Gets an existing feature or creates and sets a new instance using the default constructor.
    /// </summary>
    /// <typeparam name="TFeature">The type of the feature.</typeparam>
    /// <returns>The existing or newly created feature instance.</returns>
    /// <remarks>This method is thread-safe.</remarks>
    public TFeature GetOrSetSafe<TFeature>() where TFeature : new()
        => GetOrSetSafe(static () => new TFeature());

    internal TFeature GetOrSetSafe<TFeature>(Func<TFeature> factory)
        => _parent.GetOrSetSafe(_selectionId, factory);

    internal TFeature GetOrSetSafe<TFeature, TContext>(Func<TContext, TFeature> factory, TContext context)
        => _parent.GetOrSetSafe(_selectionId, factory, context);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        => _parent.GetFeatures(_selectionId).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
