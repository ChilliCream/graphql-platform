namespace HotChocolate.Execution
{
    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        public IServiceCollection Services { get; }

        public IQueryExecuter BuildQueryExecuter()
        {
            throw new NotImplementedException();
        }

        public IQueryExecutionBuilder Use(QueryMiddleware middleware)
        {
            throw new NotImplementedException();
        }
    }
}