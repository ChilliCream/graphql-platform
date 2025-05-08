// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

namespace HotChocolate.Features;

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
    public static TFeature GetOrSet<TFeature>(this IFeatureCollection featureCollection)
        where TFeature : new()
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
    /// Retrieves the requested feature from the collection.
    /// Throws an <see cref="InvalidOperationException"/> if the feature is not present.
    /// </summary>
    /// <param name="featureCollection">The <see cref="IFeatureCollection"/>.</param>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <returns>The requested feature.</returns>
    public static TFeature GetRequired<TFeature>(this IFeatureCollection featureCollection)
        where TFeature : notnull
    {
        ArgumentNullException.ThrowIfNull(featureCollection);

        return featureCollection.Get<TFeature>() ??
            throw new InvalidOperationException($"Feature '{typeof(TFeature)}' is not present.");
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

        return featureCollection[key] ??
            throw new InvalidOperationException($"Feature '{key}' is not present.");
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
}
