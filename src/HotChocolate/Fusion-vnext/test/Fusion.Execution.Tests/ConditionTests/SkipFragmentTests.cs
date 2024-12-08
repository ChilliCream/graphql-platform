using static HotChocolate.Language.Utf8GraphQLParser;

namespace HotChocolate.Fusion;

public class SkipFragmentTests : FusionTestBase
{
    [Test]
    public async Task Skipped_Root_Selection_Same_Selection_Without_Skip_In_Fragment()
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Selection_Same_Selection_In_Fragment()
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Selection_Same_Selection_With_Different_Skip_In_Fragment()
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Fragment()
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Fragment_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
          """
          query($slug: String!) {
              ... QueryFragment @skip(if: false)
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Fragment_If_True()
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph()
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph_If_False()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ... QueryFragment1 @skip(if: false)
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
        await MatchSnapshotAsync(request, plan);
    }

    [Test]
    public async Task Skipped_Root_Fragment_Other_Not_Skipped_Root_Fragment_From_Same_Subgraph_If_True()
    {
        // arrange
        var compositeSchema = CreateCompositeSchema();

        var request = Parse(
            """
            query($slug: String!) {
                ... QueryFragment1 @skip(if: false)
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
        await MatchSnapshotAsync(request, plan);
    }

//      [Test]
//      public async Task Skip_On_SubFragment()
//      {
//          // arrange
//          var compositeSchema = CreateCompositeSchema();
//
//          var request = Parse(
//              """
//              query($slug: String!, $skip: Boolean!) {
//                  productBySlug(slug: $slug) {
//                      ...ProductFragment @skip(if: $skip)
//                      description
//                  }
//              }
//
//              fragment ProductFragment on Product {
//                  name
//              }
//              """);
//
//          // act
//          var plan = PlanOperation(request, compositeSchema);
//
//          // assert
//          await MatchSnapshotAsync(request, plan);
//      }
//
//      [Test]
//      public async Task Skip_On_SubFragment_If_False()
//      {
//          // arrange
//          var compositeSchema = CreateCompositeSchema();
//
//          var request = Parse(
//              """
//              query($slug: String!) {
//                  productBySlug(slug: $slug) {
//                      ...ProductFragment @skip(if: false)
//                      description
//                  }
//              }
//
//              fragment ProductFragment on Product {
//                  name
//              }
//              """);
//
//          // act
//          var plan = PlanOperation(request, compositeSchema);
//
//          // assert
//          await MatchSnapshotAsync(request, plan);
//      }
//
//      [Test]
//      public async Task Skip_On_SubFragment_If_True()
//      {
//          // arrange
//          var compositeSchema = CreateCompositeSchema();
//
//          var request = Parse(
//              """
//              query($slug: String!) {
//                  productBySlug(slug: $slug) {
//                      ...ProductFragment @skip(if: true)
//                      description
//                  }
//              }
//
//              fragment ProductFragment on Product {
//                  name
//              }
//              """);
//
//          // act
//          var plan = PlanOperation(request, compositeSchema);
//
//          // assert
//          await MatchSnapshotAsync(request, plan);
//      }
//
//      [Test]
//      public async Task Skip_On_SubFragment_Only_Skipped_Fragment_Selected()
//      {
//          // arrange
//          var compositeSchema = CreateCompositeSchema();
//
//          var request = Parse(
//              """
//              query($slug: String!, $skip: Boolean!) {
//                  productBySlug(slug: $slug) {
//                      ...ProductFragment @skip(if: $skip)
//                  }
//              }
//
//              fragment ProductFragment on Product {
//                  name
//              }
//              """);
//
//          // act
//          var plan = PlanOperation(request, compositeSchema);
//
//          // assert
//          await MatchSnapshotAsync(request, plan);
//      }
//
//      [Test]
//      public async Task Skip_On_SubFragment_Only_Skipped_Fragment_SelectedIf_False()
//      {
//          // arrange
//          var compositeSchema = CreateCompositeSchema();
//
//          var request = Parse(
//              """
//              query($slug: String!) {
//                  productBySlug(slug: $slug) {
//                      ...ProductFragment @skip(if: false)
//                  }
//              }
//
//              fragment ProductFragment on Product {
//                  name
//              }
//              """);
//
//          // act
//          var plan = PlanOperation(request, compositeSchema);
//
//          // assert
//          await MatchSnapshotAsync(request, plan);
//      }
//
//      [Test]
//      public async Task Skip_On_SubFragment_Only_Skipped_Fragment_Selected_If_True()
//      {
//          // arrange
//          var compositeSchema = CreateCompositeSchema();
//
//          var request = Parse(
//              """
//              query($slug: String!) {
//                  productBySlug(slug: $slug) {
//                      ...ProductFragment @skip(if: true)
//                  }
//              }
//
//              fragment ProductFragment on Product {
//                  name
//              }
//              """);
//
//          // act
//          var plan = PlanOperation(request, compositeSchema);
//
//          // assert
//          await MatchSnapshotAsync(request, plan);
//      }
}
