using System.Collections.Generic;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProvider
    {
        IReadOnlyCollection<IFilterFieldHandler> FieldHandlers { get; }

        FieldDelegate CreateExecutor<TEntityType>(NameString argumentName);
    }

    public interface IFilterVisitorFactory<TContext>
        where TContext : ISyntaxVisitorContext
    {
        ISyntaxVisitor<TContext> CreateVisitor(FilterOperationCombinator combinator);
    }
}

