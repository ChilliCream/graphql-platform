using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        private readonly List<QueryMiddleware> _middlewareComponents =
            new List<QueryMiddleware>();

        public IServiceCollection Services { get; } = new ServiceCollection();

        public IQueryExecutionBuilder Use(QueryMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _middlewareComponents.Add(middleware);
            return this;
        }

        public IQueryExecuter Build(ISchema schema)
        {
            IServiceProvider services = Services.BuildServiceProvider();
            QueryDelegate middleware = Compile(_middlewareComponents);
            return new QueryExecuter(schema, services, middleware);
        }

        private QueryDelegate Compile(IEnumerable<QueryMiddleware> components)
        {
            QueryDelegate current = context => Task.CompletedTask;

            foreach (QueryMiddleware component in components.Reverse())
            {
                current = component(current);
            }

            return current;
        }

        public static IQueryExecutionBuilder New() =>
            new QueryExecutionBuilder();

        public static IQueryExecuter BuildDefault(ISchema schema) =>
            New().UseDefaultPipeline().Build(schema);
    }
}
