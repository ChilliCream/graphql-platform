using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using Xunit;
using static HotChocolate.Validation.TestHelper;

namespace HotChocolate.Validation
{
    public class MaxComplexityRuleTests
    {
        [InlineData(11)]
        [InlineData(10)]
        [InlineData(9)]
        [Theory]
        public void MaxComplexity_Not_Reached(int maxAllowedComplexity)
        {
            // arrange
            ExpectValid(
                CreateSchema(),
                b => b.AddMaxComplexityRule(maxAllowedComplexity),
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
        public void MaxComplexity_Reached_8()
        {
            // arrange
            ExpectErrors(
                CreateSchema(),
                b => b.AddMaxComplexityRule(8),
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

        [InlineData(19)]
        [InlineData(18)]
        [Theory]
        public void MaxComplexity_Not_Reached_With_CustomCalculateDelegate(int maxAllowedComplexity)
        {
            // arrange
            ExpectValid(
                CreateSchema(),
                b => b.AddMaxComplexityRule(maxAllowedComplexity)
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
        }

        private ISchema CreateSchema()
        {
            return SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
                .AddCostDirectiveType()
                .Use(next => context => default(ValueTask))
                .Create();
        }
    }
}
