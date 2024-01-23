using Microsoft.Extensions.DependencyInjection;
using CookieCrumble;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Types;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Tests.TestHelper;
using FileResource = ChilliCream.Testing.FileResource;

namespace HotChocolate.Execution.Pipeline;

public class ComplexityAnalyzerTests
{
    [Fact]
    public async Task MaxComplexity_Not_Reached()
    {
        var complexity = 0;

        await ExpectValid(
            @"{
                foo {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                }
            }",
            configure: b => b
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 9;
                })
                .UseRequest(next => async context =>
                {
                    await next(context);
                    complexity = (int)context.ContextData[OperationComplexity]!;
                })
                .UseDefaultPipeline());

        Assert.Equal(9, complexity);
    }

    [Fact]
    public async Task MaxComplexity_Reached()
    {
        await ExpectError(
            @"{
                foo {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                }
            }",
            configure: b => b
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 8;
                })
                .UseDefaultPipeline());
    }

    [Fact]
    public async Task Alias_Explosion_Does_Not_Kill_The_Analyzer_With_Defaults()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(
                    o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    })
                .UseDefaultPipeline()
                .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(FileResource.Open("aliases.graphql"));

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Alias_Explosion_Does_Not_Kill_The_Analyzer_With_Defaults_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(
                    o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    })
                .UseDefaultPipeline()
                .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(FileResource.Open("aliases_2048.graphql"));

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Alias_Explosion_Does_Not_Kill_The_Analyzer()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(
                    o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    })
                .ModifyParserOptions(
                    o =>
                    {
                        o.MaxAllowedFields = 40000;
                    })
                .UseDefaultPipeline()
                .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(FileResource.Open("aliases.graphql"));

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxComplexity_Analysis_Skipped()
    {
        await ExpectValid(
            @"{
                foo {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                }
            }",
            request: b => b.SkipComplexityAnalysis(),
            configure: b => b
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 8;
                })
                .UseDefaultPipeline());
    }

    [Fact]
    public async Task MaxComplexity_Analysis_Request_Maximum()
    {
        await ExpectValid(
            @"{
                foo {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                }
            }",
            request: b => b.SetMaximumAllowedComplexity(1000),
            configure: b => b
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 8;
                })
                .UseDefaultPipeline());
    }

    [Fact]
    public async Task MaxComplexity_Custom_Calculation()
    {
        var complexity = 0;

        await ExpectValid(
            @"{
                foo {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                }
            }",
            configure: b => b
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 1000;
                    o.Complexity.Calculation = context =>
                        ComplexityAnalyzerSettings.DefaultCalculation(context) * 2;
                })
                .UseRequest(next => async context =>
                {
                    await next(context);
                    complexity = (int)context.ContextData[OperationComplexity]!;
                })
                .UseDefaultPipeline());

        Assert.Equal(50, complexity);
    }

    [Fact]
    public async Task Apply_Complexity_Defaults_For_Connections()
    {
        var complexity = 0;

        await ExpectValid(
            @"{
                persons {
                    nodes {
                        name
                    }
                }
            }",
            configure: b => b
                .AddQueryType<Query>()
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 1000;
                })
                .UseRequest(next => async context =>
                {
                    await next(context);
                    complexity = (int)context.ContextData[OperationComplexity]!;
                })
                .UseDefaultPipeline());

        Assert.Equal(70, complexity);
    }

    [Fact]
    public async Task Apply_Complexity_Defaults_For_Connections_And_Resolvers()
    {
        var complexity = 0;

        await ExpectValid(
            @"{
                persons {
                    nodes {
                        name
                    }
                }
                person {
                    name
                }
            }",
            configure: b => b
                .AddQueryType<Query>()
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 1000;
                })
                .UseRequest(next => async context =>
                {
                    await next(context);
                    complexity = (int)context.ContextData[OperationComplexity]!;
                })
                .UseDefaultPipeline());

        Assert.Equal(76, complexity);
    }

    [Fact]
    public async Task Apply_Complexity_Defaults_For_Connections_And_Resolvers_And_InMemField()
    {
        var complexity = 0;

        await ExpectValid(
            @"{
                persons {
                    nodes {
                        name
                    }
                }
                person {
                    name
                }
                sayHello
            }",
            configure: b => b
                .AddQueryType<Query>()
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 1000;
                })
                .UseRequest(next => async context =>
                {
                    await next(context);
                    complexity = (int)context.ContextData[OperationComplexity]!;
                })
                .UseDefaultPipeline());

        Assert.Equal(77, complexity);
    }

    [Fact]
    public async Task Apply_Complexity_Defaults()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyRequestOptions(
                    o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    })
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task MaxComplexity_Not_With_Union()
    {
        var complexity = 0;

        await ExpectValid(
            @"{
                bazOrBar {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                    ... on Bar {
                        baz {
                            foo {
                                field
                            }
                        }
                    }
                }
            }",
            configure: b => b
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 1000;
                })
                .UseRequest(next => async context =>
                {
                    await next(context);
                    complexity = (int)context.ContextData[OperationComplexity]!;
                })
                .UseDefaultPipeline());

        Assert.Equal(16, complexity);
    }

    [Fact]
    public async Task Ensure_Cache_Is_Hit_When_Two_Ops_In_Request()
    {
        // arrange
        const string requestDocument =
            """
            query GetBazBar {
                bazOrBar {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                    ... on Bar {
                        baz {
                            foo {
                                field
                            }
                        }
                    }
                }
            }

            query FooBar {
                bazOrBar {
                    __typename
                }
            }
            """;

        var request =
            QueryRequestBuilder.New()
                .SetQuery(requestDocument);

        var diagnostics = new CacheHit();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(
                    o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    })
                .AddDiagnosticEventListener(_ => diagnostics)
                .UseDefaultPipeline()
                .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(request.SetOperation("GetBazBar").Create());
        await executor.ExecuteAsync(request.SetOperation("FooBar").Create());
        await executor.ExecuteAsync(request.SetOperation("GetBazBar").Create());
        await executor.ExecuteAsync(request.SetOperation("FooBar").Create());
        await executor.ExecuteAsync(request.SetOperation("GetBazBar").Create());
        await executor.ExecuteAsync(request.SetOperation("GetBazBar").Create());
        await executor.ExecuteAsync(request.SetOperation("GetBazBar").Create());
        await executor.ExecuteAsync(request.SetOperation("FooBar").Create());

        // assert
        Assert.Equal(2, diagnostics.Compiled);
    }

     [Fact]
    public async Task Ensure_Cache_Is_Hit_When_Single_Op()
    {
        // arrange
        const string requestDocument =
            """
            query GetBazBar {
                bazOrBar {
                    ... on Foo {
                        ... on Foo {
                            field
                            ... on Bar {
                                baz {
                                    foo {
                                        field
                                    }
                                }
                            }
                        }
                    }
                    ... on Bar {
                        baz {
                            foo {
                                field
                            }
                        }
                    }
                }
            }
            """;

        var request =
            QueryRequestBuilder.New()
                .SetQuery(requestDocument);

        var diagnostics = new CacheHit();

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .UseField(_ => _ => default)
                .ConfigureSchema(s => s.AddCostDirectiveType())
                .ModifyRequestOptions(
                    o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    })
                .UseDefaultPipeline()
                .AddDiagnosticEventListener(_ => diagnostics)
                .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(request.Create());
        await executor.ExecuteAsync(request.Create());

        // assert
        Assert.Equal(1, diagnostics.Compiled);
    }

    public class Query
    {
        [UsePaging]
        public IQueryable<Person> GetPersons()
            => new[] { new Person(), }.AsQueryable();

        public Task<Person> GetPersonAsync()
            => Task.FromResult(new Person());

        public string SayHello() => "Hello";
    }

    public class Person
    {
        public string Name { get; set; } = "Luke";
    }

    public sealed class CacheHit : ExecutionDiagnosticEventListener
    {
        public int Compiled { get; private set; }

        public override void OperationComplexityAnalyzerCompiled(IRequestContext context)
        {
            Compiled++;
        }
    }
}
