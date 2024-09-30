using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters;

/// <summary>
/// Marks a <see cref="FilterInputType"/> as an <see cref="EnumOperationFilterInputType{T}"/>.
/// This is makes the identification and the mapping of a comparable types on
/// <see cref="FilterOperationHandler{TContext,T}"/> easier.
/// <example><see cref="QueryableEnumEqualsHandler"/></example>
/// </summary>
public interface IEnumOperationFilterInputType
{
}
