using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// Marks a <see cref="FilterInputType"/> as an <see cref="ListFilterInputType{T}"/>.
    /// This is makes the identification and the mapping of a comparable types on
    /// <see cref="FilterOperationHandler{TContext,T}"/> easier.
    /// <example><see cref="QueryableListAnyOperationHandler"/></example>
    /// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IListFilterInputType
    {
    }
#pragma warning restore CA1040 // Avoid empty interfaces
}
