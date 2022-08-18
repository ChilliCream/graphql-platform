using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntegrationTests
{
    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsFieldRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    baz
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsListRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    qux
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectListRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    nestedList {
                        bar
                    }
                }
            }
            ");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectRequested()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    nested {
                        bar
                    }
                }
            }
            ");

        result.MatchSnapshot();
    }
}

public class Query
{
    [UseProjection]
    public IQueryable<Foo> Foos => new Foo[]
    {
            new() { Bar = "A" },
            new() { Bar = "B" }
    }.AsQueryable();
}

[ExtendObjectType(typeof(Foo))]
public class FooExtensions
{
    public string Baz => "baz";

    public IEnumerable<string> Qux => new[]
    {
            "baz"
        };

    public IEnumerable<Foo> NestedList => new[]
    {
            new Foo() { Bar = "C" }
        };

    public Foo Nested => new() { Bar = "C" };
}

public class Foo
{
    public string? Bar { get; set; }
}
