using System.Linq;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Options;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
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
            var complexity = 0;

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
                    .UseRequest(next => async context =>
                    {
                        await next(context);
                        complexity = (int)context.ContextData[OperationComplexity]!;
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

/*
        [Fact]
        public void MaxComplexity_Reached_With_CustomCalculateDelegate_17()
        {
            // arrange
            ExpectErrors(
                CreateSchema(),
                b => b.AddMaxComplexityRule(17)
                    .SetComplexityCalculation(
                        (field, selection, cost, fieldDepth, nodeDepth, getVariable, options) =>
                        {
                            if (cost is null)
                            {
                                return 2;
                            }
                            return cost.Complexity * 2;
                        }),
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
            ");
        }

        [Fact]
        public void MaxComplexity_Reached_With_CustomCalculateDelegate_16()
        {
            // arrange
            ExpectErrors(
                CreateSchema(),
                b => b.AddMaxComplexityRule(16)
                    .SetComplexityCalculation(
                        (field, selection, cost, fieldDepth, nodeDepth, getVariable, options) =>
                        {
                            if (cost is null)
                            {
                                return 2;
                            }
                            return cost.Complexity * 2;
                        }),
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
            ");
        }

        [InlineData(17)]
        [InlineData(16)]
        [Theory]
        public void MaxComplexity_Not_Reached_WithUnions(int maxAllowedComplexity)
        {
            // arrange
            ExpectValid(
                CreateSchema(),
                b => b.AddMaxComplexityRule(maxAllowedComplexity),
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
            ");
        }

        [Fact]
        public void MaxComplexity_Reached_WithUnions_15()
        {
            // arrange
            ExpectErrors(
                CreateSchema(),
                b => b.AddMaxComplexityRule(15),
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
            ");
        }

        [Fact]
        public void MaxComplexity_Reached_WithUnions_14()
        {
            // arrange
            ExpectErrors(
                CreateSchema(),
                b => b.AddMaxComplexityRule(15),
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
            ");
        }*/

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
