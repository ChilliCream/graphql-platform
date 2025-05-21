using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Data.Sorting;

/// <summary>
/// The configuration for the sorting feature.
/// </summary>
/// <param name="ArgumentName">
/// The argument that represents a sorting input type.
/// </param>
/// <param name="ArgumentVisitor">
/// The visitor that shall be used to visit the sorting argument.
/// </param>
public sealed record SortingFeature(
    string ArgumentName,
    VisitSortArgument ArgumentVisitor);

public static class SortingFeatureExtensions
{
    public static string? GetSortingArgumentName(this ISelection selection)
        => selection.Field.Features.Get<SortingFeature>()?.ArgumentName;

    public static bool HasSortingFeature(this ISelection selection)
    {
        return selection.Field.Features.Get<SortingFeature>() is not null;
    }
}