namespace HotChocolate.Data.Filters
{
    public class FilterMiddlewareContext
    {
        public FilterMiddlewareContext(
            IFilterConvention convention)
        {
            Convention = convention;
        }

        public IFilterConvention Convention { get; }

        public static FilterMiddlewareContext Create(
            IFilterConvention convention)
        {
            return new FilterMiddlewareContext(convention);
        }
    }
}
