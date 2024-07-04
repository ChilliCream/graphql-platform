// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

namespace HotChocolate.Features;

/// <summary>
/// An object that has features.
/// </summary>
public interface IFeatureProvider
{
    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    IFeatureCollection Features { get; }
}
