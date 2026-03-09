// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Features;

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
    /// <param name="key"></param>
    /// <returns>The requested feature, or null if it is not present.</returns>
    object? this[Type key] { get; set; }

    /// <summary>
    /// Retrieves the requested feature from the collection.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <returns>The requested feature, or null if it is not present.</returns>
    TFeature? Get<TFeature>()
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
    /// Sets the given feature in the collection.
    /// </summary>
    /// <typeparam name="TFeature">The feature key.</typeparam>
    /// <param name="instance">The feature value.</param>
    void Set<TFeature>(TFeature? instance)
        => this[typeof(TFeature)] = instance;
}
