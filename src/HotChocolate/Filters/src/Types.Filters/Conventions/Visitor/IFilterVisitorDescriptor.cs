namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterVisitorDescriptor<T> : IFluent
    {
        IFilterConventionDescriptor And();

        IFilterVisitorTypeDescriptor<T> Kind(FilterKind kind);

        IFilterCombinatorDescriptor<T> Combinator(FilterCombinator combinator);

        IFilterVisitorDescriptor<T> Middleware<TMiddleware>()
            where TMiddleware : class, IFilterMiddleware<T>, new();

        IFilterVisitorDescriptor<T> Middleware(IFilterMiddleware<T> middleware);
    }
}
