using System.Diagnostics.CodeAnalysis;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class RequestPlannerTests
{
    [Fact]
    public async Task Query_Plan_01()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_02_Aliases()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              a: users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
              b: users {
                name
                reviews {
                  body
                  author {
                    name
                  }
                }
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_03_Argument_Literals()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query GetUser {
              userById(id: 1) {
                id
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_04_Argument_Variables()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts($first: Int!) {
                topProducts(first: $first) {
                    upc
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_05_TypeName_Field()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts {
                __typename
                topProducts(first: 2) {
                    __typename
                    reviews {
                        __typename
                        author {
                            __typename
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_06_Introspection()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Introspect {
                __schema {
                    types {
                        name
                        kind
                        fields {
                            name
                            type {
                                name
                                kind
                            }
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_07_Introspection_And_Fetch()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts($first: Int!) {
                topProducts(first: $first) {
                    upc
                }
                __schema {
                    types {
                        name
                        kind
                        fields {
                            name
                            type {
                                name
                                kind
                            }
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_08_Single_Mutation()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReview {
                addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_09_Two_Mutation_Same_SubGraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReviews {
                a: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            name
                        }
                    }
                }
                b: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            name
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_10_Two_Mutation_Same_SubGraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReviews {
                a: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            birthdate
                        }
                    }
                }
                b: addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            id
                            birthdate
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_11_Two_Mutation_Two_SubGraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            mutation AddReviewAndUser {
                addReview(input: { body: "foo", authorId: 1, upc: 1 }) {
                    review {
                        body
                        author {
                            id
                            birthdate
                        }
                    }
                }
                addUser(input: { name: "foo", username: "foo", birthdate: "abc" }) {
                    user {
                        name
                        reviews {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_12_Subscription_1()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            subscription OnNewReview {
                onNewReview {
                    body
                    author {
                        name
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_13_Subscription_2()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            subscription OnNewReview {
                onNewReview {
                    body
                    author {
                        name
                        birthdate
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_14_Node_Single_Fragment()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_15_Node_Single_Fragment_Multiple_Subgraphs()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($id: ID!) {
                node(id: $id) {
                    ... on User {
                        birthdate
                        reviews {
                            body
                        }
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_16_Two_Node_Fields_Aliased()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await new FusionGraphComposer().ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(ReviewsExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            FusionFeatureFlags.NodeField);

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query FetchNode($a: ID! $b: ID!) {
                a: node(id: $a) {
                    ... on User {
                        id
                    }
                }
                b: node(id: $b) {
                    ... on User {
                        id
                    }
                }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    private static async Task<(DocumentNode UserRequest, QueryPlan QueryPlan)> CreateQueryPlanAsync(
        Skimmed.Schema fusionGraph,
        [StringSyntax("graphql")] string query)
    {
        var serviceDefinition = SchemaFormatter.FormatAsString(fusionGraph);
        var document = Parse(serviceDefinition);
        var context = FusionTypeNames.From(document);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(document, new(context))!;

        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(rewritten.ToString())
            .UseField(n => n)
            .BuildSchemaAsync();

        var serviceConfig = FusionGraphConfiguration.Load(serviceDefinition);

        var request = Parse(query);

        var operationCompiler = new OperationCompiler(new());
        var operationDef = (OperationDefinitionNode)request.Definitions.First();
        var operation = operationCompiler.Compile(
            "abc",
            operationDef,
            schema.GetOperationType(operationDef.Operation)!,
            request,
            schema);

        var queryPlanner = new QueryPlanner(serviceConfig, schema);
        var queryPlan = queryPlanner.Plan(operation);

        return (request, queryPlan);
    }
}
