using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Proves that <see cref="AnalysisPlan.HitCaseBudget"/> and the resulting
/// fallback envelope behave the same as the built-in cost analyses' case
/// budget, for a custom algebra run only through the public API.
/// </summary>
public sealed class CaseBudgetFallbackConsumerTests
{
    // Six independently @include-gated sibling fields: 2^6 - 1 = 63 splits,
    // deliberately correlated by all being reachable together at the same
    // boundary so the exact backend would need to materialize every
    // combination to answer precisely.
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
        // arrange: a tiny case budget cannot afford the 63 splits six
        // independent Booleans need, so compilation must fall back.
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

        // assert: the fallback envelope follows every still-pending Boolean
        // edge unconditionally, so it counts all six fields despite every
        // variable being false, a sound (>=) overapproximation of the exact
        // count, which correctly counts none of them.
        Assert.True(tightPlan.HitCaseBudget);
        Assert.False(roomyPlan.HitCaseBudget);
        Assert.Equal(6, fallbackCount);
        Assert.Equal(0, exactCount);
        Assert.True(fallbackCount >= exactCount);
    }
}
