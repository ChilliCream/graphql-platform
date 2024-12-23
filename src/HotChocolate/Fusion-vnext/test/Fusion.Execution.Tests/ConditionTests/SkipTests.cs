using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class SkipTests : FusionTestBase
{
    [Fact]
    public void Skipped_Root_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
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

    [Fact]
    public void Skipped_Root_Selection_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) {
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
    public void Skipped_Root_Selections_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip1) {
                    name
                }
                products @skip(if: $skip2) {
                  nodes {
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
    public void Skipped_Root_Selections_From_Same_Subgraph_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) {
                    name
                }
                products @skip(if: $skip) {
                  nodes {
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
    public void Skipped_Root_Selections_From_Different_Subgraphs()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip1: Boolean!, $skip2: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip1) {
                    name
                }
                viewer @skip(if: $skip2) {
                    displayName
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact(Skip = "Not yet supported by the planner")]
    public void Skipped_Root_Selections_From_Different_Subgraphs_Same_Variable()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) {
                    name
                }
                viewer @skip(if: $skip) {
                    displayName
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact(Skip = "Not yet supported by the planner")]
    public void Skipped_Shared_Viewer_Root_Selection_With_Sub_Selections_From_Different_Subgraphs()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($skip: Boolean!) {
                viewer @skip(if: $skip) {
                    displayName
                    reviews(first: 3) {
                        nodes {
                            body
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
    public void Skipped_Shared_byId_Root_Selection_With_Sub_Selections_From_Different_Subgraphs()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($id: ID!) {
                productById(id: $id) @skip(if: $skip) {
                    name
                    averageRating
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) {
                    name
                }
                products {
                  nodes {
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
    public void Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) {
                    name
                }
                products {
                  nodes {
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
    public void Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) @skip(if: $skip) {
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

    [Fact(Skip = "Not yet supported by the planner")]
    public void Skipped_Root_Selection_Other_Not_Skipped_Root_Selection_From_Different_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) @skip(if: true) {
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

    [Fact]
    public void Skipped_Sub_Selection()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true)
                    description
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    name @skip(if: $skip)
                    averageRating
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_Other_Not_Skipped_Sub_Selection_From_Different_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name @skip(if: true)
                    averageRating
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_From_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    averageRating @skip(if: $skip)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_From_Different_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    averageRating @skip(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_First_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    name
                    averageRating @skip(if: $skip)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_First_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    name
                    averageRating @skip(if: true)
                }
            }
            """);

        // act
        var plan = PlanOperation(request, compositeSchema);

        // assert
        plan.MatchSnapshot();
    }

    [Fact]
    public void Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!, $skip: Boolean!) {
                productBySlug(slug: $slug) {
                    averageRating @skip(if: $skip)
                    reviews(first: 10) {
                        nodes {
                            body
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

    [Fact]
    public void Skipped_Sub_Selection_From_Different_Subgraph_Other_Not_Skipped_Sub_Selection_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                productBySlug(slug: $slug) {
                    averageRating @skip(if: true)
                    reviews(first: 10) {
                        nodes {
                            body
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

    [Fact]
    public void Skipped_Sub_Selection_That_Provides_Data_For_Lookup_On_Different_Subgraph()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($id: ID!, $skip: Boolean!) {
                reviewById(id: $id) {
                    body
                    author @skip(if: $skip) {
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
    public void Skipped_Sub_Selection_That_Provides_Data_For_Lookup_On_Different_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($id: ID!) {
                reviewById(id: $id) {
                    body
                    author @skip(if: true) {
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
}
