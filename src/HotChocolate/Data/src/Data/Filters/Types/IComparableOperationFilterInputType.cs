using HotChocolate.Data.Filters.Expressions;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// Marks a <see cref="FilterInputType"/> as a <see cref="ComparableOperationFilterInputType{T}"/>.
    /// This is makes the identification and the mapping of a comparable types on
    /// <see cref="FilterOperationHandler{TContext,T}"/> easier.
    /// <example><see cref="QueryableComparableOperationHandler"/></example>
    /// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IComparableOperationFilterInputType
    {
    }
#pragma warning restore CA1040 // Avoid empty interfaces
}
