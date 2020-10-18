namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableSingleOrDefaultInterceptor
        : QueryableTakeHandlerInterceptor
    {
        public QueryableSingleOrDefaultInterceptor()
            : base(SelectionOptions.SingleOrDefault, 2)
        {
        }
    }
}
