namespace Mocha.Features;

/// <summary>
/// Extension methods for getting feature from <see cref="IFeatureCollection"/>
/// </summary>
public static class FeatureCollectionExtensions
{
    /// <summary>
    /// Retrieves the requested feature from the collection.
    /// If the feature is not present, a new instance of the feature is created and added to the collection.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="featureCollection">The <see cref="IFeatureCollection"/>.</param>
    /// <returns>The requested feature.</returns>
    public static TFeature GetOrSet<TFeature>(this IFeatureCollection featureCollection) where TFeature : new()
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        if (featureCollection.TryGet(out TFeature? feature))
        {
            return feature;
        }

        feature = new TFeature();
        featureCollection.Set(feature);
        return feature;
    }

    /// <summary>
    /// Retrieves the requested feature from the collection, or sets it to the specified value if not present.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="featureCollection">The feature collection.</param>
    /// <param name="value">The value to set if the feature is not present.</param>
    /// <returns>The existing or newly set feature.</returns>
    public static TFeature GetOrSet<TFeature>(this IFeatureCollection featureCollection, TFeature value)
        => GetOrSet(featureCollection, static state => state, value);

    /// <summary>
    /// Retrieves the requested feature from the collection, or creates and adds it using the specified factory if not present.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="featureCollection">The feature collection.</param>
    /// <param name="factory">The factory to create the feature if not present.</param>
    /// <returns>The existing or newly created feature.</returns>
    public static TFeature GetOrSet<TFeature>(this IFeatureCollection featureCollection, Func<TFeature> factory)
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        if (featureCollection.TryGet(out TFeature? feature))
        {
            return feature;
        }

        feature = factory();
        featureCollection.Set(feature);
        return feature;
    }

    /// <summary>
    /// Retrieves the requested feature from the collection, or creates and adds it using the specified factory and state if not present.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <typeparam name="TState">The type of the state passed to the factory.</typeparam>
    /// <param name="featureCollection">The feature collection.</param>
    /// <param name="factory">The factory to create the feature if not present.</param>
    /// <param name="state">The state to pass to the factory.</param>
    /// <returns>The existing or newly created feature.</returns>
    public static TFeature GetOrSet<TFeature, TState>(
        this IFeatureCollection featureCollection,
        Func<TState, TFeature> factory,
        TState state)
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        if (featureCollection.TryGet(out TFeature? feature))
        {
            return feature;
        }

        feature = factory(state);
        featureCollection.Set(feature);
        return feature;
    }

    /// <summary>
    /// Updates a feature in the collection by applying a transformation function to the existing value.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="featureCollection">The feature collection.</param>
    /// <param name="update">The function that transforms the current feature value (or <c>null</c> if not present) into the new value.</param>
    public static void Update<TFeature>(this IFeatureCollection featureCollection, Func<TFeature?, TFeature> update)
    {
        ArgumentNullException.ThrowIfNull(featureCollection);
        ArgumentNullException.ThrowIfNull(update);

        var feature = featureCollection.Get<TFeature>();
        feature = update(feature);
        featureCollection.Set(feature);
    }

    /// <summary>
    /// Retrieves the requested feature from the collection.
    /// Throws an <see cref="InvalidOperationException"/> if the feature is not present.
    /// </summary>
    /// <param name="featureCollection">The <see cref="IFeatureCollection"/>.</param>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <returns>The requested feature.</returns>
    public static TFeature GetRequired<TFeature>(this IFeatureCollection featureCollection) where TFeature : notnull
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        return featureCollection.Get<TFeature>()
            ?? throw new InvalidOperationException($"Feature '{typeof(TFeature)}' is not present.");
    }

    /// <summary>
    /// Retrieves the requested feature from the collection.
    /// Throws an <see cref="InvalidOperationException"/> if the feature is not present.
    /// </summary>
    /// <param name="featureCollection">feature collection</param>
    /// <param name="key">The feature key.</param>
    /// <returns>The requested feature.</returns>
    public static object GetRequired(this IFeatureCollection featureCollection, Type key)
    {
        ArgumentNullException.ThrowIfNull(featureCollection);
        ArgumentNullException.ThrowIfNull(key);

        return featureCollection[key] ?? throw new InvalidOperationException($"Feature '{key}' is not present.");
    }

    /// <summary>
    /// Creates a readonly collection of features.
    /// </summary>
    /// <param name="featureCollection">
    /// The <see cref="IFeatureCollection"/> to make readonly.
    /// </param>
    /// <returns>
    /// A readonly <see cref="IFeatureCollection"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="featureCollection"/> is <c>null</c>.
    /// </exception>
    public static IFeatureCollection ToReadOnly(this IFeatureCollection featureCollection)
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        if (featureCollection.IsReadOnly)
        {
            return featureCollection;
        }

        if (featureCollection.IsEmpty)
        {
            return EmptyFeatureCollection.Default;
        }

        return new ReadOnlyFeatureCollection(featureCollection);
    }

    public static void CopyTo(this IFeatureCollection source, IFeatureCollection target)
    {
        foreach (var (type, feature) in source)
        {
            target[type] = feature;
        }
    }
}
