using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// Provides extension methods for <see cref="SortingFeature"/>.
/// </summary>
public static class SortingFeatureExtensions
{
    extension(Selection selection)
    {
        /// <summary>
        /// Gets the sorting argument name from the selection.
        /// </summary>
        /// <returns>The sorting argument name.</returns>
        public string? SortingArgumentName
            => selection.Field.Features.Get<SortingFeature>()?.ArgumentName;

        /// <summary>
        /// Checks if the selection has a sorting feature.
        /// </summary>
        /// <returns>True if the selection has a sorting feature, otherwise false.</returns>
        public bool HasSortingFeature
            => selection.Field.Features.Get<SortingFeature>() is not null;
    }
}
