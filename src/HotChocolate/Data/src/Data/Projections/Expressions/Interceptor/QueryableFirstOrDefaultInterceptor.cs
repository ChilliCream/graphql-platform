namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryableFirstOrDefaultInterceptor : QueryableTakeHandlerInterceptor
    {
        public QueryableFirstOrDefaultInterceptor()
            : base(SelectionOptions.FirstOrDefault, 1)
        {
        }
    }
}
