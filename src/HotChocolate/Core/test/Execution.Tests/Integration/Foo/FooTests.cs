using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate;

public class FooTests
{
    [Fact]
    public async Task Abc()
    {
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();

        for (int i = 0; i < 10; i++)
        {
            using var cts = new CancellationTokenSource(200);

            Task a = executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ foo bar }")
                    .Create(),
                    cts.Token);

            Task b = executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ foo bar }")
                    .Create(),
                    cts.Token);

            try
            {
                await a;
                await b;
            }
            catch { }
        }
    }

    public class Query
    {
        public async Task<string> Foo()
        {
            await Task.Delay(2000);
            return "foo";
        }

        public async Task<string> Bar()
        {
            await Task.Delay(500);
            return "foo";
        }
    }
}