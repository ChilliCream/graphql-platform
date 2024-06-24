using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObjectResult = HotChocolate.Execution.Processing.ObjectResult;

namespace HotChocolate.CostAnalysis;

public sealed class SpecificationExampleTests2
{
    [Theory]
    [MemberData(nameof(SpecificationExampleData))]
    public async Task Execute_SpecificationExample_ReturnsExpectedResult(
        int index,
        string schema,
        string query,
        double expectedFieldCost)
    {
        // arrange
        query =
            $$"""
              query Example {
                  {{query}}

                  __cost {
                      requestCosts {
                          typeCost
                          fieldCost
                      }
                  }
              }
              """;

        var snapshot = new Snapshot(postFix: index.ToString());

        snapshot
            .Add(schema, "Schema")
            .Add(query, "Query");

        var requestExecutor = await CreateRequestExecutorBuilder()
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();

        // act
        var result = await requestExecutor.ExecuteAsync(query);
        var queryResult = result.ExpectQueryResult();

        snapshot.AddResult(queryResult, "Result");

        // assert
        var data = Assert.IsType<ObjectResult>(queryResult.Data);
        var cost = Assert.IsType<ObjectResult>(data.GetValueOrDefault("__cost"));
        var requestCosts = Assert.IsType<ObjectResult>(cost.GetValueOrDefault("requestCosts"));
        var fieldCost = Assert.IsType<double>(requestCosts.GetValueOrDefault("fieldCost"));

        Assert.Equal(expectedFieldCost, fieldCost);
        await snapshot.MatchMarkdownAsync();
    }

    public static TheoryData<int, string, string, double> SpecificationExampleData()
    {
        return new TheoryData<int, string, string, double>
        {
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Example
            {
                0,
                """
                type User {
                    name: String
                    age: Int @cost(weight: "2.0")
                }

                type Query {
                    users(max: Int): [User] @listSize(slicingArguments: ["max"])
                }
                """,
                """
                users(max: 5) {
                    age
                }
                """,
                11
            },
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost.Example-Argument-Weights
            // Without argument.
            {
                1,
                """
                type Query {
                    topProducts(filter: Filter @cost(weight: "15.0")): [String]
                        @cost(weight: "5.0") @listSize(assumedSize: 10)
                }

                input Filter { field: Boolean! }
                """,
                "topProducts",
                5
            },
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost.Example-Argument-Weights
            // With argument.
            {
                2,
                """
                type Query {
                    topProducts(filter: Filter @cost(weight: "15.0")): [String]
                        @cost(weight: "5.0") @listSize(assumedSize: 10)
                }

                input Filter { field: Boolean! }
                """,
                "topProducts(filter: { field: true })",
                20
            },
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost.Example-Negative-Weights
            // Without argument.
            {
                3,
                """
                type Query {
                    mostPopularProduct(approx: Approximate @cost(weight: "-3.0")): Product
                        @cost(weight: "5.0")
                }

                input Approximate { field: Boolean! }
                type Product { field: Boolean! }
                """,
                "mostPopularProduct { field }",
                5
            },
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost.Example-Negative-Weights
            // With argument.
            {
                4,
                """
                type Query {
                    mostPopularProduct(approx: Approximate @cost(weight: "-3.0")): Product
                        @cost(weight: "5.0")
                }

                input Approximate { field: Boolean! }
                type Product { field: Boolean! }
                """,
                "mostPopularProduct(approx: { field: true }) { field }",
                2
            },
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost.Example-Input-Field-Weights
            {
                5,
                """
                input Filter {
                    approx: Approximate @cost(weight: "-12.0")
                }

                type Query {
                    topProducts(filter: Filter @cost(weight: "15.0")): [String]
                        @cost(weight: "5.0") @listSize(assumedSize: 10)
                }

                input Approximate { field: Boolean! }
                """,
                "topProducts(filter: { approx: { field: true } })",
                8
            },
            // https://ibm.github.io/graphql-specs/cost-spec.html#sec-Field-Cost.Example-Directive-Arguments
            {
                6,
                """
                directive @approx(tolerance: Float! @cost(weight: "-1.0")) on FIELD

                type Query {
                    example: [String] @cost(weight: "5.0")
                }
                """,
                "example @approx(tolerance: 0)",
                5 // FIXME: Should be 4. See https://github.com/ChilliCream/graphql-platform/pull/7130.
            }
        };
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
    {
        return new ServiceCollection()
            .AddGraphQLServer()
            .UseDefaultPipelineWithCostAnalysis()
            .UseField(next => next);
    }
}
