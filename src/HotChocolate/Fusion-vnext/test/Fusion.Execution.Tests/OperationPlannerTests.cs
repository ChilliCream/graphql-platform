using HotChocolate.Fusion.Planning;
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
        plan.MatchSnapshot();
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
        plan.MatchSnapshot();
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
        plan.MatchSnapshot();
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
        plan.MatchSnapshot();
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
        plan.MatchSnapshot();
    }
}
