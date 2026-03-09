using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters;

/// <summary>
/// The configuration for the filter feature.
/// </summary>
/// <param name="ArgumentName">
/// The argument that represents a filter input type.
/// </param>
/// <param name="ArgumentVisitor">
/// The visitor that shall be used to visit the filter argument.
/// </param>
public sealed record FilterFeature(
    string ArgumentName,
    VisitFilterArgument ArgumentVisitor);
