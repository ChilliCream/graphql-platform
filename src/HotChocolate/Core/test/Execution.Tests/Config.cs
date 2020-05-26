using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class Foo
    {
        [Fact]
        public async void Something()
        {
            var services = new ServiceCollection();
            services.AddGraphQL("foo")
                .AddQueryType<Query>()
                .UseDefaultPipeline()
                .UseDocumentParser()
                .ConfigureSchemaAsync((b, ct) =>
                {
                    return default;
                });

            IRequestExecutorResolver resolver = services.BuildServiceProvider().GetRequiredService<IRequestExecutorResolver>();
            IRequestExecutor executor = await resolver.GetRequestExecutorAsync("foo");
            IExecutionResult result = await executor.ExecuteAsync(QueryRequestBuilder.New().SetQuery("{ __typename }").Create());

            result.ToJson().MatchSnapshot();
        }


        public class Query
        {
            public string Hello() => "World";
        }
    }
}