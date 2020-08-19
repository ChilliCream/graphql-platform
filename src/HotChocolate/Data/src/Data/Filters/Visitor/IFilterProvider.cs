using System.Collections.Generic;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; }

        FieldMiddleware CreateExecutor<TEntityType>(NameString argumentName);
    }

    public interface IFilterVisitorFactory<TContext>
        where TContext : IFilterVisitorContext
    {
        ISyntaxVisitor<TContext> CreateVisitor(FilterOperationCombinator combinator);
    }
}

