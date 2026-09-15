using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Proves the generic <see cref="AnalysisPlan"/> surface agrees bit-exactly
/// with the optimized <see cref="CostPlan"/> path for the built-in
/// <see cref="TupledAlgebra"/>, run through the public API only.
/// </summary>
public sealed class TupledAlgebraBitExactTests
{
    [Fact]
    public void Evaluate_Should_MatchCostPlan_Bitexactly_When_RunningTupledAlgebraThroughTheGenericPlan()
    {
        // arrange: c5-signed-weights.json, whose only variables are the Booleans
        // gating each merged "book" selection, so the generic path (which, unlike
        // CostPlan, resolves list-size slicing statically) computes the same
        // numbers as CostPlan for this fixture.
        var fixture = PrecisionFixture.Load(
            System.IO.Path.Combine(AppContext.BaseDirectory, "__resources__", "precision", "c5-signed-weights.json"));
        var schema = SchemaParser.Parse(fixture.Sdl);
        var document = Utf8GraphQLParser.Parse(fixture.Operation);
        var operation = document.Definitions
            .OfType<OperationDefinitionNode>()
            .Single(definition => definition.Name?.Value == fixture.OperationName);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var variables = FixtureVariables.Read(fixture.Variables);
        var costPlan = CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);
        var analysisPlan = AnalysisPlanCompiler.Compile(snapshot, document, operation);

        // act
        var expected = costPlan.Evaluate(variables);
        var actual = analysisPlan.Evaluate(new TupledAlgebra(snapshot), variables);

        // assert
        Assert.Equal(expected.FieldCost, actual.FieldCost);
        Assert.Equal(expected.TypeCost, actual.TypeCost);
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(expected.FieldCost),
            BitConverter.DoubleToInt64Bits(actual.FieldCost));
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(expected.TypeCost),
            BitConverter.DoubleToInt64Bits(actual.TypeCost));
    }
}
