using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class OperationPlannerTests
{
    [Test]
    public async Task Plan_Simple_Operation_1_Source_Schema()
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
            }
            """);

        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // act
        var planner = new OperationPlanner(compositeSchema);
        var plan = planner.CreatePlan(rewritten, null);

        // assert
        await Assert
            .That(plan.ToSyntaxNode().ToString(indented: true))
            .IsEqualTo(
                """
                {
                  productById(id: 1) {
                    id
                    name
                  }
                }
                """);
    }

    [Test]
    public async Task Plan_Simple_Operation_2_Source_Schema()
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
                estimatedDelivery(postCode: "12345")
            }
            """);

        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // act
        var planner = new OperationPlanner(compositeSchema);
        var plan = planner.CreatePlan(rewritten, null);

        // assert
        await Assert
            .That(plan.ToSyntaxNode().ToString(indented: true))
            .IsEqualTo(
                """
                {
                  productById(id: 1) {
                    id
                    name
                  }
                }

                {
                  productById {
                    estimatedDelivery(postCode: "12345")
                  }
                }
                """);
    }

    [Test]
    public async Task Plan_Simple_Operation_3_Source_Schema()
    {
        var compositeSchemaDoc = Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql"));
        var compositeSchema = CompositeSchemaBuilder.Create(compositeSchemaDoc);

        var doc = Utf8GraphQLParser.Parse(
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

        var rewriter = new InlineFragmentOperationRewriter(compositeSchema);
        var rewritten = rewriter.RewriteDocument(doc, null);

        // act
        var planner = new OperationPlanner(compositeSchema);
        var plan = planner.CreatePlan(rewritten, null);

        // assert
        await Assert
            .That(plan.ToSyntaxNode().ToString(indented: true))
            .IsEqualTo(
                """
                {
                  productById(id: 1) {
                    name
                  }
                }

                {
                  productById {
                    reviews(first: 10) {
                      nodes {
                        body
                        stars
                        author
                      }
                    }
                  }
                }

                {
                  userById {
                    displayName
                  }
                }
                """);
    }
}
