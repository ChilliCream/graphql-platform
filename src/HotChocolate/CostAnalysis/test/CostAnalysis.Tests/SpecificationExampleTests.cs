using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class SpecificationExampleTests
{
    [Theory]
    [MemberData(nameof(SpecificationExampleData))]
    public async Task Execute_SpecificationExample_ReturnsExpectedResult(
        int index,
        string schema,
        string operation,
        double expectedFieldCost)
    {
        // arrange
        var snapshot = new Snapshot(postFix: index.ToString());

        operation =
            $$"""
              query Example {
                  {{operation}}
              }
              """;

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();

        // act
        var result = await requestExecutor.ExecuteAsync(request);
        var queryResult = result.ExpectOperationResult();

        // assert
        await snapshot
            .Add(Utf8GraphQLParser.Parse(operation), "Query")
            .Add(expectedFieldCost, "ExpectedFieldCost")
            .AddResult(queryResult, "Result")
            .Add(schema, "Schema")
            .MatchMarkdownAsync();
    }

    public static TheoryData<int, string, string, double> SpecificationExampleData()
        => new TheoryData<int, string, string, double>
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

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .UseField(next => next);
}
