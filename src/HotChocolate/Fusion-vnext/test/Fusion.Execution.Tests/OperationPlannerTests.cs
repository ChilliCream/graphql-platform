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
                  productById {
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
                  productById {
                    id
                    name
                  }
                }

                {
                  productById {
                    estimatedDelivery
                  }
                }
                """);
    }
}
