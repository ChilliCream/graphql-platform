using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Proves that <see cref="CostSchemaIndexOptions.CaseBudgetExceededBehavior"/>
/// selects between the two documented <see cref="CostPlan"/> behaviors once
/// the case budget is exhausted, exercised only through the public option
/// and the public <see cref="CostPlanCompiler"/>/<see cref="CostPlan"/>
/// surface, the way a third-party consumer would.
/// </summary>
public sealed class CaseBudgetExceededBehaviorConsumerTests
{
    // Six independently @include-gated, distinctly weighted sibling fields:
    // 2^6 - 1 = 63 splits, more than the tight case budget below can afford.
    private const string Sdl =
        """
        directive @cost(weight: String!) on FIELD_DEFINITION
        type Query {
          f0: Int @cost(weight: "1")
          f1: Int @cost(weight: "2")
          f2: Int @cost(weight: "4")
          f3: Int @cost(weight: "8")
          f4: Int @cost(weight: "16")
          f5: Int @cost(weight: "32")
        }
        """;

    private const string Operation =
        """
        query(
          $v0: Boolean! $v1: Boolean! $v2: Boolean!
          $v3: Boolean! $v4: Boolean! $v5: Boolean!
        ) {
          f0 @include(if: $v0)
          f1 @include(if: $v1)
          f2 @include(if: $v2)
          f3 @include(if: $v3)
          f4 @include(if: $v4)
          f5 @include(if: $v5)
        }
        """;

    [Fact]
    public void Evaluate_Should_ReturnTheExactResult_When_DefaultBehaviorHitsTheCaseBudget()
    {
        // arrange: the default option value is EvaluatePerRequest.
        var schema = SchemaParser.Parse(Sdl);
        var document = Utf8GraphQLParser.Parse(Operation);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var tightSchemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions { CaseBudget = 4 });
        var roomySchemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions { CaseBudget = 4096 });
        var tightPlan = CostPlanCompiler.Compile(tightSchemaIndex, document, operation, CostAnalyses.Cost);
        var roomyPlan = CostPlanCompiler.Compile(roomySchemaIndex, document, operation, CostAnalyses.Cost);
        var allFalse = new LiteralCostVariableValues(new Dictionary<string, IValueNode>
        {
            ["v0"] = BooleanValueNode.False,
            ["v1"] = BooleanValueNode.False,
            ["v2"] = BooleanValueNode.False,
            ["v3"] = BooleanValueNode.False,
            ["v4"] = BooleanValueNode.False,
            ["v5"] = BooleanValueNode.False
        });

        // act
        var exact = tightPlan.Evaluate(allFalse);
        var reference = roomyPlan.Evaluate(allFalse);

        // assert: despite hitting the budget, the default mode still matches an unaffected
        // (roomy-budget) compile exactly.
        Assert.True(tightPlan.HitCaseBudget);
        Assert.False(roomyPlan.HitCaseBudget);
        Assert.Equal(reference, exact);
    }

    [Fact]
    public void Evaluate_Should_OverapproximateWithTheFallbackEnvelope_When_OverestimateBehaviorHitsTheCaseBudget()
    {
        // arrange: selecting Overestimate explicitly restores today's compiled-envelope behavior.
        var schema = SchemaParser.Parse(Sdl);
        var document = Utf8GraphQLParser.Parse(Operation);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var tightSchemaIndex = CostSchemaIndex.Create(
            schema,
            new CostSchemaIndexOptions
            {
                CaseBudget = 4,
                CaseBudgetExceededBehavior = CaseBudgetExceededBehavior.Overestimate
            });
        var roomySchemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions { CaseBudget = 4096 });
        var tightPlan = CostPlanCompiler.Compile(tightSchemaIndex, document, operation, CostAnalyses.Cost);
        var roomyPlan = CostPlanCompiler.Compile(roomySchemaIndex, document, operation, CostAnalyses.Cost);
        var allFalse = new LiteralCostVariableValues(new Dictionary<string, IValueNode>
        {
            ["v0"] = BooleanValueNode.False,
            ["v1"] = BooleanValueNode.False,
            ["v2"] = BooleanValueNode.False,
            ["v3"] = BooleanValueNode.False,
            ["v4"] = BooleanValueNode.False,
            ["v5"] = BooleanValueNode.False
        });

        // act
        var envelope = tightPlan.Evaluate(allFalse);
        var exact = roomyPlan.Evaluate(allFalse);

        // assert: the fallback envelope follows every still-pending Boolean edge unconditionally,
        // so it overapproximates the exact (all-false) result rather than matching it.
        Assert.True(tightPlan.HitCaseBudget);
        Assert.False(roomyPlan.HitCaseBudget);
        Assert.Equal(0.0, exact.FieldCost);
        Assert.True(envelope.FieldCost > exact.FieldCost);
    }
}
