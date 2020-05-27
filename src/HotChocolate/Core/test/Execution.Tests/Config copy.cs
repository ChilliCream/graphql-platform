using ChilliCream.Testing;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class Foo2
    {
        [Fact]
        public async void Something()
        {
            var services = new ServiceCollection();
            services
                .AddGraphQL("Foo")
                .AddQueryType<Query>();


            var resolver = services.BuildServiceProvider().GetRequiredService<IRequestExecutorResolver>();
            var executor = await resolver.GetRequestExecutorAsync("Foo");
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New().SetQuery("{ __typename }").Create());
            
            result.ToJson().MatchSnapshot();
        }



    }

    public class Query
    {
        public string Foo { get; } = "Test";

    }
}