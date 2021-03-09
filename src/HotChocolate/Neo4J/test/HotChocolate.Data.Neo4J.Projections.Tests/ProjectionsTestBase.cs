using System.Threading.Tasks;
using HotChocolate.Data.Neo4J.Execution;
using HotChocolate.Data.Neo4J.Paging;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Squadron;

namespace HotChocolate.Data.Neo4J.Projections
{
    public class ProjectionsTestBase
    {
        protected static async Task<IRequestExecutor> CreateSchema<TEntity>(
            Neo4jResource neo4JResource,
            string query,
            bool usePaging = false,
            bool useOffsetPaging = false,
            ObjectType<TEntity>? objectType = null)
            where TEntity : class
        {
            IAsyncSession session = neo4JResource.GetAsyncSession();
            IResultCursor cursor = await session.RunAsync(query);
            await cursor.ConsumeAsync();

            IRequestExecutorBuilder builder = new ServiceCollection().AddGraphQL();

            if (objectType is {})
            {
                builder.AddType(objectType);
            }

            return builder
                .AddNeo4JProjections()
                //.AddNeo4JFiltering()
                //.AddNeo4JSorting()
                .AddQueryType(
                    new ObjectType<StubObject<TEntity>>(
                        c =>
                        {
                            c.Name("Query");
                            ApplyConfigurationToFieldDescriptor<TEntity>(
                                c.Field(x => x.Root).Resolver(new Neo4JExecutable<TEntity>(neo4JResource.GetAsyncSession())),
                                usePaging,
                                useOffsetPaging);
                        }))
                .UseRequest(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IReadOnlyQueryResult result &&
                            context.ContextData.TryGetValue("query", out var queryString))
                        {
                            context.Result =
                                QueryResultBuilder
                                    .FromResult(result)
                                    .SetContextData("query", queryString)
                                    .Create();
                        }
                    })
                .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .Result;
        }

        private static void ApplyConfigurationToFieldDescriptor<TEntity>(
            IObjectFieldDescriptor descriptor,
            bool usePaging = false,
            bool useOffsetPaging = false)
        {
            if (usePaging)
            {
                descriptor.UseNeo4JPaging<ObjectType<TEntity>>();
            }

            if (useOffsetPaging)
            {
                descriptor.UseNeo4JOffsetPaging<ObjectType<TEntity>>();
            }

            descriptor
                .Use(
                    next => async context =>
                    {
                        await next(context);
                        if (context.Result is IExecutable executable)
                        {
                            context.ContextData["query"] = executable.Print();
                        }
                    })
                .UseProjection();
            //.UseFiltering()
            //.UseSorting();
        }

        public class StubObject<T>
        {
            public T Root { get; set; }
        }
    }
}
