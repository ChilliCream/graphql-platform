using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class SkipFragmentTests : FusionTestBase
{
    [Test]
    public void Skipped_Root_Selection_Same_Selection_Without_Skip_In_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) {
                    name
                }
                ... QueryFragment
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

    [Test]
    public void Skipped_Root_Selection_Same_Skipped_Selection_In_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) {
                    name
                }
                ... QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) @skip(if: $skip) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skipped_Root_Selection_Same_Skipped_Selection_With_Different_Skip_In_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip1) {
                    name
                }
                ... QueryFragment
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) @skip(if: $skip2) {
                    name
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skipped_Root_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                ... QueryFragment @skip(if: $skip)
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

    [Test]
    public void Skipped_Root_Fragment_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ... QueryFragment @skip(if: true)
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

    [Test]
    public void Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                ... QueryFragment1 @skip(if: $skip)
                ... QueryFragment2
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

    [Test]
    public void Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ... QueryFragment1 @skip(if: true)
                ... QueryFragment2
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

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                ... QueryFragment1 @skip(if: $skip)
                ... QueryFragment2
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

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Different_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ... QueryFragment1 @skip(if: true)
                ... QueryFragment2
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

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragment_With_Selections_From_Two_Subgraphs()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                ... QueryFragment @skip(if: $skip)
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
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

    [Test]
    public void Skipped_Root_Fragment_With_Selections_From_Two_Subgraphs_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ... QueryFragment @skip(if: true)
            }

            fragment QueryFragment on Query {
                productBySlug(slug: $slug) {
                    name
                }
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

    [Test]
    public void Skipped_Root_Fragments_With_Same_Root_Selection_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
                ... QueryFragment1 @skip(if: $skip1)
                ... QueryFragment2 @skip(if: $skip2)
            }

            fragment QueryFragment1 on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                productBySlug(slug: $slug) {
                    name
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skipped_Root_Fragments_With_Same_Root_Selection_From_Same_Subgraph_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                ... QueryFragment1 @skip(if: $skip)
                ... QueryFragment2 @skip(if: $skip)
            }

            fragment QueryFragment1 on Query {
                productBySlug(slug: $slug) {
                    name
                }
            }

            fragment QueryFragment2 on Query {
                productBySlug(slug: $slug) {
                    name
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragments_With_Different_Root_Selection_From_Different_Subgraphs()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
                ... QueryFragment1 @skip(if: $skip1)
                ... QueryFragment2 @skip(if: $skip2)
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

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragments_With_Different_Root_Selection_From_Different_Subgraphs_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                ... QueryFragment1 @skip(if: $skip)
                ... QueryFragment2 @skip(if: $skip)
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

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragments_With_Shared_Viewer_Selection_And_Sub_Selections_From_Two_Subgraphs_Each()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($skip1: Boolean!, $skip2: Boolean!) {
                ... QueryFragment1 @skip(if: $skip1)
                ... QueryFragment2 @skip(if: $skip2)
            }

            fragment QueryFragment1 on Query {
                viewer {
                   displayName
                   reviews {
                       nodes {
                           body
                       }
                   }
                }
            }

            fragment QueryFragment2 on Query {
                viewer {
                   reviews {
                       nodes {
                           body
                       }
                   }
                    displayName
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    [Skip("Not yet supported by the planner")]
    public void Skipped_Root_Fragments_With_Shared_Viewer_Selection_And_Sub_Selections_From_Two_Subgraphs_Each_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($skip: Boolean!) {
                ... QueryFragment1 @skip(if: $skip)
                ... QueryFragment2 @skip(if: $skip)
            }

            fragment QueryFragment1 on Query {
                viewer {
                   displayName
                   reviews {
                       nodes {
                           body
                       }
                   }
                }
            }

            fragment QueryFragment2 on Query {
                viewer {
                   reviews {
                       nodes {
                           body
                       }
                   }
                    displayName
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skipped_Sub_Selection_Same_Selection_Without_Skip_In_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    name
                    ... ProductFragment @skip(if: $skip)
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

    [Test]
    public void Skipped_Sub_Selection_Same_Skipped_Selection_In_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip)
                    ... ProductFragment
                }
            }

            fragment ProductFragment on Product {
                name @skip(if: $skip)
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skipped_Sub_Selection_Same_Skipped_Selection_With_Different_Skip_In_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip1)
                    ... ProductFragment
                }
            }

            fragment ProductFragment on Product {
                name @skip(if: $skip2)
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Test]
    public void Skipped_Sub_Fragment()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment @skip(if: $skip)
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

    [Test]
    public void Skipped_Sub_Fragment_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment @skip(if: true)
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

    [Test]
    public void Skipped_Sub_Fragment_Other_Not_Skipped_Sub_Fragment_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment1 @skip(if: $skip)
                    ... ProductFragment2
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

    [Test]
    public void Skipped_Sub_Fragment_Other_Not_Skipped_Sub_Fragment_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment1 @skip(if: true)
                    ... ProductFragment2
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

    [Test]
    public void Skipped_Sub_Fragment_Other_Not_Skipped_Sub_Fragment_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment1 @skip(if: $skip)
                    ... ProductFragment2
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

    [Test]
    public void Skipped_Sub_Fragment_Other_Not_Skipped_Sub_Fragment_From_Different_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment1 @skip(if: true)
                    ... ProductFragment2
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

    [Test]
    public void Skipped_Sub_Fragment_From_Different_Subgraph_Other_Not_Skipped_Sub_Fragment_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment1
                    ... ProductFragment2 @skip(if: $skip)
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

    [Test]
    public void Skipped_Sub_Fragment_From_Different_Subgraph_Other_Not_Skipped_Sub_Fragment_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment1
                    ... ProductFragment2 @skip(if: true)
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



    [Test]
    public void Skipped_Sub_Fragment_With_Fields_From_Different_Subgraphs()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    ... ProductFragment @skip(if: $skip)
                }
            }

            fragment ProductFragment on Product {
                name
                averageRating
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }
}
