using System;

namespace HotChocolate.Data.Filters
{
    public interface IFilterProviderDescriptor<T, TContext> : IFluent
        where TContext : FilterVisitorContext<T>
    {
        IFilterProviderDescriptor<T, TContext> AddFieldHandler<TFieldHandler>()
            where TFieldHandler : FilterFieldHandler<T, TContext>;

        IFilterProviderDescriptor<T, TContext> AddFieldHandler<TFieldHandler>(TFieldHandler handler)
            where TFieldHandler : FilterFieldHandler<T, TContext>;

        IFilterProviderDescriptor<T, TContext> Visitor<TVisitor>()
            where TVisitor : FilterVisitor<T, TContext>;

        IFilterProviderDescriptor<T, TContext> Visitor<TVisitor>(TVisitor handler)
            where TVisitor : FilterVisitor<T, TContext>;

        IFilterProviderDescriptor<T, TContext> Combinator<TCombinator>(TCombinator handler)
            where TCombinator : FilterOperationCombinator;

        IFilterProviderDescriptor<T, TContext> Combinator<TCombinator>()
            where TCombinator : FilterOperationCombinator;
    }
}