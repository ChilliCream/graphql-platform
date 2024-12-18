using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class FragmentTests : FusionTestBase
{
    [Fact]
    public void Fragment_On_Root()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ...QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Root_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                }
                ...QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Root_Next_To_Same_Selection_With_Different_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    description
                }
                ...QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Root_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                products {
                    nodes {
                        description
                    }
                }
                ...QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact(Skip = "Not yet supported by the planner")]
    public void Fragment_On_Root_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                viewer {
                    displayName
                }
                ...QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_Fragments_On_Root_With_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ...QueryFragment1
                ...QueryFragment2
            }

            fragment QueryFragment1 on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_Fragments_On_Root_With_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ...QueryFragment1
                ...QueryFragment2
            }

            fragment QueryFragment1 on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                products {
                    nodes {
                        description
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact(Skip = "Not yet supported by the planner")]
    public void Two_Fragments_On_Root_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ...QueryFragment1
                ...QueryFragment2
            }

            fragment QueryFragment1 on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                viewer {
                    displayName
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment
                }
            }

            fragment ProductFragment on Product {
                name
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Sub_Selection_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    ...ProductFragment
                }
            }

            fragment ProductFragment on Product {
                name
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Sub_Selection_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    ...ProductFragment
                }
            }

            fragment ProductFragment on Product {
                description
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Fragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    ...ProductFragment
                }
            }

            fragment ProductFragment on Product {
                averageRating
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_Fragments_On_Sub_Selection_With_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
            }

            fragment ProductFragment2 on Product {
                name
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_Fragments_On_Sub_Selection_With_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
            }

            fragment ProductFragment2 on Product {
                description
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_Fragments_On_Sub_Selection_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                name
            }

            fragment ProductFragment2 on Product {
                averageRating
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_Fragments_On_Sub_Selection_With_Different_But_Same_Entry_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ...ProductFragment1
                    ...ProductFragment2
                }
            }

            fragment ProductFragment1 on Product {
                reviews {
                    nodes {
                        body
                    }
                }
            }

            fragment ProductFragment2 on Product {
                reviews {
                    pageInfo {
                        hasNextPage
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }
}
