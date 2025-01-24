using System.Linq.Expressions;

namespace HotChocolate.Data;

/// <summary>
/// Represents a sort operation on a field.
/// </summary>
/// <typeparam name="T">
/// The entity type on which the sort operation is applied.
/// </typeparam>
/// <param name="KeySelector">
/// The field on which the sort operation is applied.
/// </param>
/// <param name="Ascending">
/// Specifies the sort directive.
/// If <c>true</c> the sort is ascending, otherwise descending.
/// </param>
public sealed record SortBy<T>(
    Expression<Func<T, object>> KeySelector,
    bool Ascending = true);
