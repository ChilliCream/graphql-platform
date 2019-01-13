using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public class QueryExecutionBuilder
        : IQueryExecutionBuilder
    {
        private readonly List<QueryMiddleware> _middlewareComponents =
            new List<QueryMiddleware>();

        private readonly List<FieldMiddleware> _fieldMiddlewareComponents =
            new List<FieldMiddleware>();

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

        public IQueryExecutionBuilder UseField(FieldMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _fieldMiddlewareComponents.Add(middleware);
            return this;
        }

        public IQueryExecuter Build(ISchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            IServiceProvider services = CopyServiceCollection()
                .AddSingleton<ISchema>(schema)
                .BuildServiceProvider();

            QueryDelegate middleware = Compile(_middlewareComponents);

            return new QueryExecuter(schema, services, middleware);
        }

        private ServiceCollection CopyServiceCollection()
        {
            var copy = new ServiceCollection();

            foreach (ServiceDescriptor descriptor in Services)
            {
                ((IList<ServiceDescriptor>)copy).Add(descriptor);
            }

            return copy;
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

        public static IQueryExecuter BuildDefault(ISchema schema,
            IQueryExecutionOptionsAccessor options) =>
                New().UseDefaultPipeline(options).Build(schema);
    }
}
