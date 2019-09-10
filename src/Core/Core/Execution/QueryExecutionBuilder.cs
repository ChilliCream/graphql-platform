using System;
using System.Collections.Generic;
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

        public IQueryExecutor Build(ISchema schema)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            IServiceProvider services = CopyServiceCollection()
                .AddSingleton(schema)
                .BuildServiceProvider();

            return new QueryExecutor
            (
                schema,
                services,
                Compile(_middlewareComponents),
                Compile(_fieldMiddlewareComponents)
            );
        }

        public void Populate(IServiceCollection services) =>
            Populate(services, false);

        public void Populate(IServiceCollection services, bool lazyExecutor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var list = (IList<ServiceDescriptor>)services;

            foreach (ServiceDescriptor descriptor in Services)
            {
                list.Add(descriptor);
            }

            services.AddSingleton<IQueryExecutor>(sp =>
            {
                if (lazyExecutor)
                {
                    return new LazyQueryExecutor
                    (
                        () => new QueryExecutor
                        (
                            sp.GetRequiredService<ISchema>(),
                            Compile(_middlewareComponents),
                            Compile(_fieldMiddlewareComponents)
                        )
                    );
                }
                else
                {
                    return new QueryExecutor
                    (
                        sp.GetRequiredService<ISchema>(),
                        Compile(_middlewareComponents),
                        Compile(_fieldMiddlewareComponents)
                    );
                }
            });
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

        private static QueryDelegate Compile(
            IReadOnlyList<QueryMiddleware> components)
        {
            QueryDelegate next = context => Task.CompletedTask;

            for (var i = components.Count - 1; i >= 0; i--)
            {
                next = components[i].Invoke(next);
            }

            return next;
        }

        private static FieldMiddleware Compile(
            IReadOnlyList<FieldMiddleware> components)
        {
            return first =>
            {
                FieldDelegate next = first;

                for (var i = components.Count - 1; i >= 0; i--)
                {
                    next = components[i].Invoke(next);
                }

                return next;
            };
        }

        public static QueryExecutionBuilder New() =>
            new QueryExecutionBuilder();

        public static IQueryExecutor BuildDefault(ISchema schema) =>
            New().UseDefaultPipeline().Build(schema);

        public static IQueryExecutor BuildDefault(ISchema schema,
            IQueryExecutionOptionsAccessor options) =>
                New().UseDefaultPipeline(options).Build(schema);

        public static void BuildDefault(
            IServiceCollection services) =>
            New().UseDefaultPipeline().Populate(services);

        public static void BuildDefault(
            IServiceCollection services,
            IQueryExecutionOptionsAccessor options) =>
                New().UseDefaultPipeline(options).Populate(services);
    }
}
