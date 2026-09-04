using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Nodes;

public sealed class PolicyArtifactBinderTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_ValidatePolicyTopology_When_DeferredRequirementUsesBatchParent()
    {
        // arrange
        using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(
                    new TestPolicy(
                        "CanReadReviews",
                        Utf8GraphQLParser.Syntax.ParseSelectionSet("{ productSku }"))))
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            ComposeSchemaDocument(
                """
                # name: a
                type Query {
                  first: Product!
                  second: Product!
                }

                type Product @key(fields: "id") {
                  id: ID!
                }
                """,
                """
                # name: b
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  productSku: String!
                }
                """,
                """
                # name: c
                enum PolicyDenialBehavior { NULL ERROR ABORT }

                directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                  repeatable on OBJECT | FIELD_DEFINITION

                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  reviews(productSku: String! @require(field: "productSku")): [String!]!
                    @policy(names: "CanReadReviews", onDenied: NULL)
                }
                """),
            services);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              first {
                id
                ... @defer {
                  reviews
                }
              }
              second {
                productSku
              }
            }
            """);

        // assert
        var incrementalPlan = Assert.Single(plan.IncrementalPlans);
        var policyNode = Assert.Single(incrementalPlan.AllNodes.OfType<PolicyExecutionNode>());
        var batch = Assert.Single(
            plan.AllNodes.OfType<OperationBatchExecutionNode>(),
            node => node.SchemaName == "b");
        Assert.Equal(
            [true],
            batch.Operations.ToArray().Select(operation => operation.Requirements.Length > 0).ToArray());
        Assert.Equal([batch.Id], policyNode.ParentDependencies.ToArray());
    }
}
