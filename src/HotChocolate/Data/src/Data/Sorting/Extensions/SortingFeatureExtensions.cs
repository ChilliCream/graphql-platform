using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Provides extension methods for <see cref="SortingFeature"/>.
/// </summary>
public static class SortingFeatureExtensions
{
    /// <summary>
    /// Gets the sorting argument name from the selection.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <returns>The sorting argument name.</returns>
    public static string? GetSortingArgumentName(this ISelection selection)
        => selection.Field.Features.Get<SortingFeature>()?.ArgumentName;

    /// <summary>
    /// Checks if the selection has a sorting feature.
    /// </summary>
    /// <param name="selection">The selection.</param>
    /// <returns>True if the selection has a sorting feature, otherwise false.</returns>
    public static bool HasSortingFeature(this ISelection selection)
        => selection.Field.Features.Get<SortingFeature>() is not null;
}
