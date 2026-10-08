using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests case-budget fallback with a custom analysis through the public API.
/// </summary>
public sealed class CaseBudgetFallbackConsumerTests
{
    // Six independent Boolean conditions require 63 splits, exceeding the tight budget.
    private const string Sdl =
        """
        type Query {
          f0: Int
          f1: Int
          f2: Int
          f3: Int
          f4: Int
          f5: Int
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
    public void Evaluate_Should_OverapproximateWithTheFallbackEnvelope_When_TheCaseBudgetIsExhausted()
    {
        // arrange
        var schema = SchemaParser.Parse(Sdl);
        var document = Utf8GraphQLParser.Parse(Operation);
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();
        var tightSchemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions { CaseBudget = 4 });
        var roomySchemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions { CaseBudget = 4096 });
        var tightPlan = AnalysisPlanCompiler.Compile(tightSchemaIndex, document, operation);
        var roomyPlan = AnalysisPlanCompiler.Compile(roomySchemaIndex, document, operation);
        var allFalse = FixtureVariables.Read(variables: null);

        // act
        var fallbackCount = tightPlan.Evaluate(new FieldCountAlgebra(), allFalse);
        var exactCount = roomyPlan.Evaluate(new FieldCountAlgebra(), allFalse);

        // assert
        // The fallback includes unresolved alternatives, even when their variables are false.
        Assert.True(tightPlan.HitCaseBudget);
        Assert.False(roomyPlan.HitCaseBudget);
        Assert.Equal(6, fallbackCount);
        Assert.Equal(0, exactCount);
        Assert.True(fallbackCount >= exactCount);
    }
}
