using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Nodes3;
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

    private static ImmutableList<PlanStep> PlanOperation(DocumentNode request, FusionSchemaDefinition schema)
    {
        var rewriter = new InlineFragmentOperationRewriter(schema);
        var rewritten = rewriter.RewriteDocument(request, null);
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();

        var planner = new OperationPlanner(schema);
        return planner.CreatePlan(operation);
    }

    private static void Match(ImmutableList<Nodes3.PlanStep> plan)
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
}
