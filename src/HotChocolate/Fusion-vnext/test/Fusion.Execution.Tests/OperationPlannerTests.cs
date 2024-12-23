using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class OperationPlannerTests : FusionTestBase
{
    [Fact]
    public void Plan_Simple_Operation_1_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

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
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Plan_Simple_Operation_2_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

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
        plan.MatchSnapshot();
    }

    [Fact]
    public void Plan_Simple_Operation_3_Source_Schema()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

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
        plan.MatchSnapshot();
    }

    [Fact]
    public void Plan_Simple_Operation_3_Source_Schema_And_Single_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query GetProduct($slug: String! $first: Int! = 10) {
                productBySlug(slug: $slug) {
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

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Plan_With_Conditional_InlineFragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
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

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }
}
