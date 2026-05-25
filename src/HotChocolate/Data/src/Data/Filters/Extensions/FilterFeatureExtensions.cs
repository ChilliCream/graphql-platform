using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Extension methods for the <see cref="FilterFeature"/> class.
/// </summary>
public static class FilterFeatureExtensions
{
    /// <summary>
    /// Checks if the selection has a filtering enabled.
    /// </summary>
    /// <param name="selection">The selection that shall be checked.</param>
    /// <returns>
    /// <c>true</c> if the selection has a filtering enabled;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool HasFilterFeature(this Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return selection.Field.Features.Get<FilterFeature>() is not null;
    }

    /// <summary>
    /// Gets the filter feature from the selection.
    /// </summary>
    /// <param name="selection">The selection that shall be checked.</param>
    /// <returns>
    /// The filter feature from the selection;
    /// otherwise, <c>null</c>.
    /// </returns>
    public static FilterFeature? GetFilterFeature(this Selection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return selection.Field.Features.Get<FilterFeature>();
    }
}
