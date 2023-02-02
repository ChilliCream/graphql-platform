using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class UseServiceScopeAttributeTests
{
    [Fact]
    public async Task UseServiceScope()
    {
        // arrange
        // assert
        var result = await new ServiceCollection()
            .AddScoped<Service>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ a: scoped b: scoped }");

        // assert
        var queryResult = result.ExpectQueryResult();
        Assert.NotEqual(queryResult.Data!["a"], queryResult.Data!["b"]);
    }

    [Fact]
    public void UseServiceScope_FieldDescriptor()
        => Assert.Throws<ArgumentNullException>(
            () => default(IObjectFieldDescriptor).UseServiceScope());

    [Fact]
    public async Task UseServiceScope_With_DataLoader()
    {
        // arrange
        using var cts = new CancellationTokenSource(5000);

        // assert
        var result = await new ServiceCollection()
            .AddScoped<Service>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ExecuteRequestAsync("{ scopeWithDataLoader }", cancellationToken: cts.Token);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "scopeWithDataLoader": "abc"
              }
            }
            """);

        cts.Cancel();
    }

    public class Query
    {
        [UseServiceScope]
        public string GetScoped([Service] Service service)
            => service.Id;

        [UseServiceScope]
        public Task<string> ScopeWithDataLoader(IResolverContext context, CancellationToken ct)
        {
            var dataLoader =
                context.BatchDataLoader<string, string>(
                    (keys, _) => Task.FromResult<IReadOnlyDictionary<string, string>>(
                        keys.ToDictionary(a => a, a => a)));
            return dataLoader.LoadAsync("abc", ct);
        }
    }

    public class Service
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }
}
