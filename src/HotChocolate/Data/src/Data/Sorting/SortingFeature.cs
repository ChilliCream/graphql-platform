using HotChocolate.Data.Sorting.Expressions;

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
