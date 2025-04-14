using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerTests : FusionTestBase
{
    [Fact]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
                productBySlug(slug: "1") {
                    ... Product
                }
            }

            fragment Product on Product {
                id
                name
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: PRODUCTS
                operation: >-
                  {
                      productBySlug(slug: "1") {
                      id
                      name
                      }
                  }
            """);
    }

    [Fact]
    public void Plan_Simple_Operation_2_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
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
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: PRODUCTS
                operation: >-
                  {
                    productBySlug(slug: "1") {
                      id
                      name
                      dimension {
                        height
                        width
                      }
                    }
                  }
              - id: 2
                schema: SHIPPING
                operation: >-
                  {
                    productById(id: $__fusion_1_id) {
                      estimatedDelivery(postCode: "12345", height: $__fusion_2_height, width: $__fusion_2_width)
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: productBySlug
                    selectionMap: id
                  - name: __fusion_2_height
                    selectionSet: productBySlug
                    selectionMap: dimension.height
                  - name: __fusion_2_width
                    selectionSet: productBySlug
                    selectionMap: dimension.width
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Simple_Operation_3_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateSchema();

        // act
        var plan = PlanOperation(
            compositeSchema,
            """
            {
                productBySlug(slug: "1") {
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
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: PRODUCTS
                operation: >-
                  {
                    productBySlug(slug: "1") {
                      name
                      id
                    }
                  }
              - id: 2
                schema: REVIEWS
                operation: >-
                  {
                    productById(id: $__fusion_1_id) {
                      reviews(first: 10) {
                        nodes {
                          body
                          stars
                          author {
                            id
                          }
                        }
                      }
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: productBySlug
                    selectionMap: id
                dependencies:
                  - id: 1
              - id: 3
                schema: ACCOUNTS
                operation: >-
                  {
                    userById(id: $__fusion_2_id) {
                      displayName
                    }
                  }
                requirements:
                  - name: __fusion_2_id
                    selectionSet: productBySlug.author.nodes.reviews
                    selectionMap: id
                dependencies:
                  - id: 2
            """);
    }

    [Fact]
    public void Plan_Simple_Lookup()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              name: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              price: Float!
            }

            directive @lookup on FIELD_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
            - id: 1
              schema: A
              operation: >-
              query GetTopProducts_1 {
                topProducts {
                  id
                  name
                }
              }
            - id: 2
              schema: B
              operation: >-
              query GetTopProducts_2 {
                productById(id: $__fusion_1_id) {
                  price
                }
              }
              requirements:
                - name: __fusion_1_id
                  selectionSet: topProducts
                  selectionMap: id
              dependencies:
                - id: 1
            """);
    }

    [Fact]
    public void Plan_Simple_Requirement()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              name: String!
              region: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }

            directive @lookup on FIELD_DEFINITION

            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // assert
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: A
                operation: >-
                  query GetTopProducts_1 {
                    topProducts {
                      id
                      name
                      region
                    }
                  }
              - id: 2
                schema: B
                operation: >-
                  query GetTopProducts_2 {
                    productById(id: $__fusion_1_id) {
                      price(region: $__fusion_2_region)
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: topProducts
                    selectionMap: id
                  - name: __fusion_2_region
                    selectionSet: topProducts
                    selectionMap: region
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Plan_Requirement_That_Cannot_Be_Inlined()
    {
        // arrange
        var schema = ComposeSchema(
            """
            schema @schemaName(value: "A") {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product {
              id: ID!
              name: String!
              region: String!
            }

            directive @schemaName(value: String!) on SCHEMA
            """,
            """
            schema @schemaName(value: "B") {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }

            directive @lookup on FIELD_DEFINITION

            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            directive @schemaName(value: String!) on SCHEMA
            """);

        // assert
        var plan = PlanOperation(
            schema,
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            nodes:
              - id: 1
                schema: A
                operation: >-
                  query GetTopProducts_1 {
                    topProducts {
                      id
                      name
                      region
                    }
                  }
              - id: 2
                schema: B
                operation: >-
                  query GetTopProducts_2 {
                    productById(id: $__fusion_1_id) {
                      price(region: $__fusion_2_region)
                    }
                  }
                requirements:
                  - name: __fusion_1_id
                    selectionSet: topProducts
                    selectionMap: id
                  - name: __fusion_2_region
                    selectionSet: topProducts
                    selectionMap: region
                dependencies:
                  - id: 1
            """);
    }

    private ExecutionPlan PlanOperation(
        FusionSchemaDefinition schema,
        [StringSyntax("graphql")] string operationText)
    {
        var operationDoc = Parse(operationText);

        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(operationDoc, null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var planner = new OperationPlanner(schema);
        var steps = planner.CreatePlan(operation);

        var test = new TestMe();
        return test.BuildExecutionTree(operation, steps.OfType<OperationPlanStep>().ToImmutableList());
    }

    private static ImmutableList<PlanStep> PlanOperation(FusionSchemaDefinition schema, DocumentNode request)
    {
        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(request, null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var planner = new OperationPlanner(schema);
        return planner.CreatePlan(operation);
    }

    private static void Match(ImmutableList<PlanStep> plan)
    {
        var i = 0;
        var snapshot = new Snapshot();

        foreach (var step in plan)
        {
            switch (step)
            {
                case OperationPlanStep operation:
                    snapshot.Add(
                        operation.Definition.ToString(),
                        $"{++i} {operation.SchemaName}",
                        markdownLanguage: MarkdownLanguages.GraphQL);
                    break;
            }
        }

        snapshot.MatchMarkdown();
    }

    private static void MatchInline(
        ExecutionPlan plan,
        [StringSyntax("yaml")] string expected)
    {
        var formatter = new YamlExecutionPlanFormatter();
        var actual = formatter.Format(plan);
        actual.MatchInlineSnapshot(expected + Environment.NewLine);
    }

    private static void MatchInline(
        ImmutableList<PlanStep> plan,
        string expected)
    {
        var i = 0;
        var snapshot = new Snapshot();

        foreach (var step in plan)
        {
            switch (step)
            {
                case OperationPlanStep operation:
                    snapshot.Add(
                        operation.Definition.ToString(),
                        $"{++i} {operation.SchemaName}",
                        markdownLanguage: MarkdownLanguages.GraphQL);
                    break;
            }
        }

        snapshot.MatchInline(expected);
    }
}
