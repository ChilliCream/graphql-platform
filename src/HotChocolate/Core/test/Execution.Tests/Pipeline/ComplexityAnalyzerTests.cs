using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.Execution.Options;
using HotChocolate.Tests;
using HotChocolate.Types;
using Xunit;
using static HotChocolate.WellKnownContextData;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Pipeline
{
    public class ComplexityAnalyzerTests
    {
        [Fact]
        public async Task MaxComplexity_Not_Reached()
        {
            var complexity = 0;

            await ExpectValid(
                @"
                    {
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
                    }
                ",
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
                @"
                    {
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
                    }
                ",
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
        public async Task MaxComplexity_Analysis_Skipped()
        {
            await ExpectValid(
                @"
                    {
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
                    }
                ",
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
        public async Task MaxComplexity_Custom_Calculation()
        {
            var complexity = 0;

            await ExpectValid(
                @"
                    {
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
                    }
                ",
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
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyRequestOptions(o =>
                {
                    o.Complexity.Enable = true;
                    o.Complexity.MaximumAllowed = 1000;
                })
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task MaxComplexity_Not_With_Union()
        {
            var complexity = 0;

            await ExpectValid(
                @"
                    {
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
                ",
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

        private static ISchema CreateSchema()
        {
            return SchemaBuilder.New()
                .AddDocumentFromString(
                    FileResource.Open("CostSchema.graphql"))
                .AddCostDirectiveType()
                .Use(_ => _ => default)
                .Create();
        }

        public class Query
        {
            [UsePaging]
            public IQueryable<Person> GetPersons() =>
                new[] { new Person() }.AsQueryable();

            public Task<Person> GetPersonAsync() =>
                Task.FromResult(new Person());

            public string SayHello() => "Hello";
        }

        public class Person
        {
            public string Name { get; set; } = "Luke";
        }
    }
}
