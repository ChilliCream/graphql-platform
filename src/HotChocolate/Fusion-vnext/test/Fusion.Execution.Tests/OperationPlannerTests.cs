using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class OperationPlannerTests : FusionTestBase
{
    [Test]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // assert
        plan.ToJson().MatchInlineSnapshot(
            """
            {
              "nodes": [
                {
                  "id": 1,
                  "schema": "PRODUCTS",
                  "operation": "{ productById(id: 1) { id name } }"
                }
              ]
            }
            """);
    }

    [Test]
    public void Plan_Simple_Operation_2_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
                estimatedDelivery(postCode: "12345")
            }
            """);

        // assert
        plan.ToJson().MatchInlineSnapshot(
            """
            {
              "nodes": [
                {
                  "id": 1,
                  "schema": "PRODUCTS",
                  "operation": "{ productById(id: 1) { id name id } }"
                },
                {
                  "id": 2,
                  "schema": "SHIPPING",
                  "operation": "query($__fusion_requirement_1: ID!) { productById(id: $__fusion_requirement_1) { estimatedDelivery(postCode: \u002212345\u0022) } }",
                  "requirements": [
                    {
                      "name": "__fusion_requirement_1",
                      "dependsOn": 1,
                      "field": [
                        "productById"
                      ],
                      "type": "ID!"
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Test]
    public void Plan_Simple_Operation_3_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            {
                productById(id: 1) {
                    ... ProductCard
                }
            }

            fragment ProductCard on Product {
                name
                reviews(first: 10) {
                    nodes {
                        ... ReviewCard
                    }
                }
            }

            fragment ReviewCard on Review {
                body
                stars
                author {
                    ... AuthorCard
                }
            }

            fragment AuthorCard on UserProfile {
                displayName
            }
            """);

        // assert
        plan.ToJson().MatchInlineSnapshot(
            """
            {
              "nodes": [
                {
                  "id": 1,
                  "schema": "PRODUCTS",
                  "operation": "{ productById(id: 1) { name id } }"
                },
                {
                  "id": 2,
                  "schema": "REVIEWS",
                  "operation": "query($__fusion_requirement_2: ID!) { productById(id: $__fusion_requirement_2) { reviews(first: 10) { nodes { body stars author { id } } } } }",
                  "requirements": [
                    {
                      "name": "__fusion_requirement_2",
                      "dependsOn": 1,
                      "field": [
                        "productById"
                      ],
                      "type": "ID!"
                    }
                  ]
                },
                {
                  "id": 3,
                  "schema": "ACCOUNTS",
                  "operation": "query($__fusion_requirement_1: ID!) { userById(id: $__fusion_requirement_1) { displayName } }",
                  "requirements": [
                    {
                      "name": "__fusion_requirement_1",
                      "dependsOn": 2,
                      "field": [
                        "author",
                        "nodes",
                        "reviews",
                        "productById"
                      ],
                      "type": "ID!"
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Test]
    public void Plan_Simple_Operation_3_Source_Schema_And_Single_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        // act
        var plan = PlanOperationAsync(
            compositeSchema,
            """
            query GetProduct($id: ID!, $first: Int! = 10) {
                productById(id: $id) {
                    ... ProductCard
                }
            }

            fragment ProductCard on Product {
                name
                reviews(first: $first) {
                    nodes {
                        ... ReviewCard
                    }
                }
            }

            fragment ReviewCard on Review {
                body
                stars
                author {
                    ... AuthorCard
                }
            }

            fragment AuthorCard on UserProfile {
                displayName
            }
            """);

        // assert
        plan.ToJson().MatchInlineSnapshot(
            """
            {
              "nodes": [
                {
                  "id": 1,
                  "schema": "PRODUCTS",
                  "operation": "query($id: ID!) { productById(id: $id) { name id } }"
                },
                {
                  "id": 2,
                  "schema": "REVIEWS",
                  "operation": "query($__fusion_requirement_2: ID!, $first: Int! = 10) { productById(id: $__fusion_requirement_2) { reviews(first: $first) { nodes { body stars author { id } } } } }",
                  "requirements": [
                    {
                      "name": "__fusion_requirement_2",
                      "dependsOn": 1,
                      "field": [
                        "productById"
                      ],
                      "type": "ID!"
                    }
                  ]
                },
                {
                  "id": 3,
                  "schema": "ACCOUNTS",
                  "operation": "query($__fusion_requirement_1: ID!) { userById(id: $__fusion_requirement_1) { displayName } }",
                  "requirements": [
                    {
                      "name": "__fusion_requirement_1",
                      "dependsOn": 2,
                      "field": [
                        "author",
                        "nodes",
                        "reviews",
                        "productById"
                      ],
                      "type": "ID!"
                    }
                  ]
                }
              ]
            }
            """);
    }

    [Test]
    public void Plan_With_Conditional_InlineFragment()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

        var doc = Utf8GraphQLParser.Parse(
            """
            {
                productById(id: 1) {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
                ... @include(if: true) {
                    estimatedDelivery(postCode: "12345")
                }
            }
            """);

        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // act
        var planner = new OperationPlanner(compositeSchema);
        var plan = planner.CreatePlan(rewritten, null);

        // assert
        plan.ToJson().MatchInlineSnapshot(
            """
            {
              "nodes": [
                {
                  "id": 1,
                  "schema": "PRODUCTS",
                  "operation": "{ productById(id: 1) { id name } }"
                }
              ]
            }
            """);
    }
}
