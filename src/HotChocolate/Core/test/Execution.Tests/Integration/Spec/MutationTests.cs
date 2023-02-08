using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;

namespace HotChocolate.Execution.Integration.Spec;

public class MutationTests
{
    [Fact]
    public async Task Ensure_Mutations_Are_Executed_Serially()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("a").Resolve("b"))
                .AddMutationType<Mutation>()
                .ExecuteRequestAsync(
                    """
                    mutation {
                        a
                        b
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": 1,
                "b": 2
              }
            }
            """);
    }

    public class Mutation
    {
        private int _order;
        private bool _a;
        private bool _b;
        private readonly object _sync = new();

        public async Task<int> A()
        {
            lock (_sync)
            {
                if (_b)
                {
                    throw new GraphQLException("B");
                }

                _a = true;
            }

            await Task.Delay(100);
            _a = false;
            return Interlocked.Increment(ref _order);
        }

        public async Task<int> B()
        {
            lock (_sync)
            {
                if (_a)
                {
                    throw new GraphQLException("B");
                }

                _b = true;
            }

            await Task.Delay(100);
            _b = false;
            return Interlocked.Increment(ref _order);
        }
    }
}
