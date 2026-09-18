using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Proves the generic <see cref="AnalysisPlan"/> surface agrees bit-exactly
/// with the optimized <see cref="CostPlan"/> path for the built-in
/// <see cref="TupledAlgebra"/>, run through the public API only, over every
/// precision fixture with that fixture's own variables. Constructing
/// <see cref="TupledAlgebra"/> with those variables is what makes a
/// variable-bound slicing argument (see fixture "c2-list-size-variable")
/// agree with <see cref="CostPlan"/> as well, not just Boolean-only
/// fixtures.
/// </summary>
public sealed class TupledAlgebraBitExactTests
{
    public static TheoryData<string> PrecisionFixtures => PrecisionFixtureLoader.FixturePaths();

    [Theory]
    [MemberData(nameof(PrecisionFixtures))]
    public void Evaluate_Should_MatchCostPlan_Bitexactly_When_RunningTupledAlgebraThroughTheGenericPlan(string path)
    {
        // arrange
        var fixture = PrecisionFixture.Load(path);
        var schema = SchemaParser.Parse(fixture.Sdl);
        var document = Utf8GraphQLParser.Parse(fixture.Operation);
        var operation = document.Definitions
            .OfType<OperationDefinitionNode>()
            .Single(definition => definition.Name?.Value == fixture.OperationName);
        var schemaIndex = CostSchemaIndex.Create(schema, new CostSchemaIndexOptions());
        var variables = FixtureVariables.Read(fixture.Variables);
        var costPlan = CostPlanCompiler.Compile(schemaIndex, document, operation, CostAnalyses.Cost);
        var analysisPlan = AnalysisPlanCompiler.Compile(schemaIndex, document, operation);

        // act: TupledAlgebra constructed WITH the fixture's own variables is
        // what lets its direct slicing-argument and input-cost resolution
        // match CostPlan's coerced resolution for variable-bound fixtures.
        var expected = costPlan.Evaluate(variables);
        var actual = analysisPlan.Evaluate(new TupledAlgebra(schemaIndex, variables), variables);

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
