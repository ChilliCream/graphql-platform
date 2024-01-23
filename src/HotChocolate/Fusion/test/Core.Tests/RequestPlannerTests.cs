using System.Diagnostics.CodeAnalysis;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Composition.Features;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Shared;
using HotChocolate.Language;
using HotChocolate.Skimmed.Serialization;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using HttpClientConfiguration = HotChocolate.Fusion.Composition.HttpClientConfiguration;

namespace HotChocolate.Fusion;

public class RequestPlannerTests
{
    [Fact]
    public async Task Query_Plan_01()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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
    public async Task Query_Plan_05_TypeName_Field()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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
                    id
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

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

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

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

    [Fact]
    public async Task Query_Plan_17_Multi_Completion()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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
                birthdate
              }
              reviews {
                body
              }
              __schema {
                types {
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
    public async Task Query_Plan_18_Node_Single_Fragment_Multiple_Subgraphs()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl)
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

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
                    ... on Review {
                        body
                        author {
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
    public async Task Query_Plan_19_Requires()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    name
                    deliveryEstimate(zip: "12345") {
                      min
                      max
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
    public async Task Query_Plan_20_DeepQuery()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
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
                    birthdate
                    reviews {
                      body
                      author {
                        name
                        birthdate
                      }
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
    public async Task Query_Plan_21_Field_Requirement_Not_In_Context()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    deliveryEstimate(zip: "12345") {
                      min
                      max
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
    public async Task Query_Plan_22_Interfaces()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
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
    public async Task Query_Plan_23_Interfaces_Merge()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
                demoProject.Patient1.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Appointments {
              appointments {
                nodes {
                  patient {
                    id
                    ... on Patient1 {
                        name
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
    public async Task Query_Plan_24_Field_Requirement_And_Fields_In_Context()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Requires {
                reviews {
                  body
                  author {
                    name
                    birthdate
                  }
                  product {
                    id
                    name
                    deliveryEstimate(zip: "12345") {
                      min
                      max
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
    public async Task Query_Plan_25_Variables_Are_Passed_Through()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Appointment.ToConfiguration(),
                demoProject.Patient1.ToConfiguration(),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Appointments($first: Int!) {
              patientById(patientId: 1) {
                name
                appointments(first: $first) {
                    nodes {
                        id
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
    public async Task Query_Plan_26_Ensure_No_Circular_Dependency_When_Requiring_Data()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl),
            },
            new FusionFeatureCollection(FusionFeatures.NodeField));

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query TopProducts {
              topProducts(first: 5) {
                weight
                deliveryEstimate(zip: "12345") {
                  min
                  max
                }
                reviews {
                  body
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
    public async Task Query_Plan_27_Multiple_Require_Steps_From_Same_Subgraph()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                demoProject.Authors.ToConfiguration(),
                demoProject.Books.ToConfiguration()
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                authorById(id: "1") {
                    id,
                    name,
                    bio,
                    books {
                        id
                        author {
                            books {
                                id
                            }
                        }
                    }
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_28_Simple_Root_Data()
    {
        // arrange
        var schemaA =
            """
            type Query {
                data: Data
            }

            type Data {
                a: String
            }

            schema {
                query: Query
            }
            """;

        var schemaB =
            """
            type Query {
                data: Data
            }

            type Data {
                b: String
            }

            schema {
                query: Query
            }
            """;

        var fusionGraph = await FusionGraphComposer.ComposeAsync(new[]
        {
            new SubgraphConfiguration("A", schemaA, Array.Empty<string>(), CreateClients(), null),
            new SubgraphConfiguration("B", schemaB, Array.Empty<string>(), CreateClients(), null)
        });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                data {
                    a
                    b
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_29_Simple_Root_List_Data()
    {
        // arrange
        var schemaA =
            """
            type Query {
                data: [Data]
            }

            type Data {
                a: String
            }

            schema {
                query: Query
            }
            """;

        var schemaB =
            """
            type Query {
                data: [Data]
            }

            type Data {
                b: String
            }

            schema {
                query: Query
            }
            """;

        var fusionGraph = await FusionGraphComposer.ComposeAsync(new[]
        {
            new SubgraphConfiguration("A", schemaA, Array.Empty<string>(), CreateClients(), null),
            new SubgraphConfiguration("B", schemaB, Array.Empty<string>(), CreateClients(), null)
        });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                data {
                    a
                    b
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_30_Entity_Data()
    {
        // arrange
        var schemaA =
            """
            type Query {
                entity(id: ID!): Entity
            }

            type Entity {
                id: ID!
                a: String
            }

            schema {
                query: Query
            }
            """;

        var schemaB =
            """
            type Query {
                entity(id: ID!): Entity
            }

            type Entity {
                id: ID!
                b: String
            }

            schema {
                query: Query
            }
            """;

        var fusionGraph = await FusionGraphComposer.ComposeAsync(new[]
        {
            new SubgraphConfiguration("A", schemaA, Array.Empty<string>(), CreateClients(), null),
            new SubgraphConfiguration("B", schemaB, Array.Empty<string>(), CreateClients(), null)
        });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Query {
                entity(id: 123) {
                    a
                    b
                }
            }
            """);

        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_32_Argument_No_Value_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_33_Argument_Default_Value_Explicitly_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg(arg: VALUE2)
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_34_Argument_Not_Default_Value_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg(arg: VALUE1)
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_35_Argument_Value_Variable_Specified()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test($variable: TestEnum) {
              fieldWithEnumArg(arg: $variable)
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Query_Plan_31_Argument_No_Value_Specified_With_Selection_Set()
    {
        // arrange
        var fusionGraph = await FusionGraphComposer.ComposeAsync(
            new[]
            {
                new SubgraphConfiguration(
                    "Test",
                    """
                    type Query {
                        fieldWithEnumArg(arg: TestEnum = VALUE2): TestObject
                    }

                    type TestObject {
                        test: Boolean
                    }

                    enum TestEnum {
                        VALUE1,
                        VALUE2
                    }
                    """,
                    "",
                    new []
                    {
                        new HttpClientConfiguration(new Uri("http://client"), "Test"),
                    },
                    null),
            });

        // act
        var result = await CreateQueryPlanAsync(
            fusionGraph,
            """
            query Test {
              fieldWithEnumArg {
                test
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(result.UserRequest, nameof(result.UserRequest));
        snapshot.Add(result.QueryPlan, nameof(result.QueryPlan));
        await snapshot.MatchAsync();
    }

    private static async Task<(DocumentNode UserRequest, Execution.Nodes.QueryPlan QueryPlan)> CreateQueryPlanAsync(
        Skimmed.Schema fusionGraph,
        [StringSyntax("graphql")] string query)
    {
        var document = SchemaFormatter.FormatAsDocument(fusionGraph);
        var context = FusionTypeNames.From(document);
        var rewriter = new FusionGraphConfigurationToSchemaRewriter();
        var rewritten = rewriter.Rewrite(document, new(context))!;

        var services = new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(rewritten.ToString())
            .UseField(n => n);

        if (document.Definitions.Any(d => d is ScalarTypeDefinitionNode { Name.Value: "Upload", }))
        {
            services.AddUploadType();
        }

        var schema = await services.BuildSchemaAsync();
        var serviceConfig = FusionGraphConfiguration.Load(document);

        var request = Parse(query);

        var operationCompiler = new OperationCompiler(new());
        var operationDef = (OperationDefinitionNode)request.Definitions[0];
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

    private static IClientConfiguration[] CreateClients()
        =>
        [
            new HttpClientConfiguration(new Uri("http://nothing")),
        ];
}
