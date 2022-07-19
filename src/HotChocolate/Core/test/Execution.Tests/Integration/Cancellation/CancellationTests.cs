using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate;

public class CancellationTests
{
    [Fact]
    public async Task Ensure_Execution_Waits_For_Tasks()
    {
        // arrange
        var query = new Query();

        var executor =
            await new ServiceCollection()
                .AddSingleton(query)
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        using var cts = new CancellationTokenSource(150);

        // act
        await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ foo bar }")
                .Create(),
                cts.Token);

        // assert
        Assert.True(query.Foo);
        Assert.True(query.FooDone);
        Assert.True(query.Bar);
    }

    public class Query
    {
        [GraphQLIgnore]
        public bool Foo { get; set; }

        [GraphQLIgnore]
        public bool FooDone { get; set; }

        [GraphQLIgnore]
        public bool Bar { get; set; }

        [GraphQLIgnore]
        public bool Baz { get; set; }

        [Serial]
        public async Task<string> GetFoo()
        {
            Foo = true;
            await Task.Delay(400);
            FooDone = true;
            return "foo";
        }

        [Serial]
        public async Task<string> GetBar()
        {
            Bar = true;
            await Task.Delay(200);
            return "bar";
        }
    }
}
