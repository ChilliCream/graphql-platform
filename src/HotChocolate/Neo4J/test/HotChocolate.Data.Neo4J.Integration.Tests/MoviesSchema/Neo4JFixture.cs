using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data.Neo4J.Integration
{
    public class Neo4JFixture : Neo4jResource<Neo4JConfig>
    {
        public IRequestExecutor CreateSchema()
        {
            return new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Name("Query"))
                    .AddType<Queries>()
                .AddNeo4JProjections()
                .AddNeo4JFiltering()
                .AddNeo4JSorting()
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>()
                .GetRequestExecutorAsync()
                .GetAwaiter()
                .GetResult();
        }
    }
}
