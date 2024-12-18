using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class InlineFragmentTests : FusionTestBase
{
    [Fact]
    public void InlineFragment_On_Root()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void InlineFragment_On_Root_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                }
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void InlineFragment_On_Root_Next_To_Same_Selection_With_Different_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    description
                }
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void InlineFragment_On_Root_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                products {
                    nodes {
                        description
                    }
                }
                ... {
                    productBySlug(slug: $slug) {
                        name
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
    public void InlineFragment_On_Root_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                viewer {
                    displayName
                }
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_InlineFragments_On_Root_With_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Two_InlineFragments_On_Root_With_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
                ... {
                    products {
                        nodes {
                            description
                        }
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
    public void Two_InlineFragments_On_Root_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                ... {
                    productBySlug(slug: $slug) {
                        name
                    }
                }
                ... {
                    viewer {
                        displayName
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void InlineFragment_On_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void InlineFragment_On_Sub_Selection_Next_To_Same_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    ... {
                        name
                    }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void InlineFragment_On_Sub_Selection_Next_To_Different_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    ... {
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

    [Fact]
    public void InlineFragment_On_Sub_Selection_Next_To_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    ... {
                        averageRating
                    }
                }
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

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... {
                        name
                    }
                    ... {
                        name
                    }
                }
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

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    ... {
                        name
                    }
                    ... {
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

    [Fact]
    public void Two_Fragments_On_Sub_Selection_With_Different_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                   ... {
                       name
                   }
                   ... {
                       averageRating
                   }
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Merge_Fields_With_Alias()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Utf8GraphQLParser.Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                   ... {
                       a: name
                   }
                   a: name
                   name
                   ... {
                       name
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
