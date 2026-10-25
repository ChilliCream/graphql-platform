using System.Diagnostics.CodeAnalysis;

namespace Mocha;

/// <summary>
/// Represents a collection of GraphQL features.
/// </summary>
public interface IFeatureCollection : IEnumerable<KeyValuePair<Type, object>>
{
    /// <summary>
    /// Indicates if the collection can be modified.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Indicates if the collection is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Incremented for each modification and can be used to verify cached results.
    /// </summary>
    int Revision { get; }

    /// <summary>
    /// Gets or sets a given feature. Setting a null value removes the feature.
    /// </summary>
    /// <param name="key">The type of the feature to get or set.</param>
    /// <returns>The requested feature, or null if it is not present.</returns>
    object? this[Type key] { get; set; }

    /// <summary>
    /// Retrieves the requested feature from the collection.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <returns>The requested feature, or null if it is not present.</returns>
    TFeature? Get<TFeature>();

    /// <summary>
    /// Tries to retrieve the requested feature from the collection.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="feature">
    /// The requested feature, or null if it is not present.
    /// </param>
    /// <returns>
    /// <c>true</c> if the feature is present; otherwise, <c>false</c>.
    /// </returns>
    bool TryGet<TFeature>([NotNullWhen(true)] out TFeature? feature);

    /// <summary>
    /// Retrieves the requested feature, creating it when no instance is available.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <returns>The requested feature.</returns>
    TFeature GetOrSet<TFeature>() where TFeature : new()
        => GetOrSet<TFeature, object?>(static _ => new TFeature(), null);

    /// <summary>
    /// Retrieves the requested feature, adding the specified value when no instance is available.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="value">The value to add when no instance is available.</param>
    /// <returns>The requested feature.</returns>
    TFeature GetOrSet<TFeature>(TFeature value)
        => GetOrSet(static value => value, value);

    /// <summary>
    /// Retrieves the requested feature, invoking the factory when no instance is available.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="factory">The factory used when no instance is available.</param>
    /// <returns>The requested feature.</returns>
    TFeature GetOrSet<TFeature>(Func<TFeature> factory)
        => GetOrSet(static factory => factory(), factory);

    /// <summary>
    /// Retrieves the requested feature, invoking the factory with the specified state when no instance is available.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <typeparam name="TState">The type of the state passed to the factory.</typeparam>
    /// <param name="factory">The factory used when no instance is available.</param>
    /// <param name="state">The state passed to the factory.</param>
    /// <returns>The requested feature.</returns>
    TFeature GetOrSet<TFeature, TState>(Func<TState, TFeature> factory, TState state)
    {
        if (TryGet(out TFeature? feature))
        {
            return feature;
        }

        feature = factory(state);
        Set(feature);
        return feature;
    }

    /// <summary>
    /// Sets the given feature in the collection.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="instance">The feature value.</param>
    void Set<TFeature>(TFeature? instance);
}
