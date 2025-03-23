using System.Collections.Immutable;
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

        var request = Parse(
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

        // act
        var plan = PlanOperation(request, schema);

        // assert
        Match(plan);
    }

    [Fact]
    public void Plan_Simple_Operation_2_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateSchema();

        var request = Parse(
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

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        Match(plan);
    }

    [Fact]
    public void Plan_Simple_Operation_3_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateSchema();

        var request = Parse(
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

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        Match(plan);
    }

    [Fact]
    public void Plan_Simple_Lookup()
    {
        // arrange
        var compositeSchema = CreateSchema(
            """
            type Query
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              topProducts: [Product!]
                @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: B
                key: "{ id }"
                field: "productById(id: ID!): Product"
                map: ["id"]
              ) {
              id: ID!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              name: String!
                @fusion__field(schema: A)
              price: Float!
                @fusion__field(schema: B)
            }

            enum fusion__Schema {
              A
              B
            }
            """);

        // assert
        var request = Parse(
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        var plan = PlanOperation(request, compositeSchema);

        // assert
        MatchInline(
            plan,
            """
            1 A
            ---------------
            {
              topProducts {
                id
                name
              }
            }
            ---------------

            2 B
            ---------------
            {
              productById {
                price
              }
            }
            ---------------
            """);
    }

    [Fact]
    public void Plan_Simple_Requirement()
    {
        // arrange
        var compositeSchema = CreateSchema(
            """
            type Query
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              topProducts: [Product!]
                @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: B
                key: "{ id }"
                field: "productById(id: ID!): Product"
                map: ["id"]
              ) {
              id: ID!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              name: String!
                @fusion__field(schema: A)
              price: Float!
                @fusion__field(schema: B)
                @fusion__requires(
                  schema: B
                  field: "price(region: String!): Int!"
                  map: ["region"]
                )
              region: String!
                @fusion__field(schema: C)
            }

            enum fusion__Schema {
              A
              B
              C
            }
            """);

        // assert
        var request = Parse(
            """
            query GetTopProducts {
              topProducts {
                id
                name
                price
              }
            }
            """);

        var plan = PlanOperation(request, compositeSchema);

        // assert
        MatchInline(
            plan,
            """
            1 A
            ---------------
            {
              topProducts {
                id
                name
              }
            }
            ---------------

            2 B
            ---------------
            {
              productById {
                price
              }
            }
            ---------------
            """);
    }

    private static ImmutableList<PlanStep> PlanOperation(DocumentNode request, FusionSchemaDefinition schema)
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
