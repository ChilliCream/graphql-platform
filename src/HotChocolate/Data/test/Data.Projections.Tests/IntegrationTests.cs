using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data;

public class IntegrationTests
{
    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsFieldRequested()
    {
        // arrange
        // act
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    baz
                }
            }
            ");

        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsListRequested()
    {
        // arrange
        // act
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    qux
                }
            }
            ");

        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectListRequested()
    {
        // arrange
        // act
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    nestedList {
                        bar
                    }
                }
            }
            ");

        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Projection_Should_NotBreakProjections_When_ExtensionsObjectRequested()
    {
        // arrange
        // act
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<FooExtensions>()
            .AddProjections()
            .BuildRequestExecutorAsync();

        // assert
        IExecutionResult result = await executor.ExecuteAsync(@"
            {
                foos {
                    bar
                    nested {
                        bar
                    }
                }
            }
            ");

        result.ToJson().MatchSnapshot();
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
