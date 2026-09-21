using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests that <see cref="TupledAlgebra"/> and <see cref="CostPlan"/> produce identical estimates.
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

        // act
        // The algebra needs the same variables as the plan to resolve slicing arguments and input costs.
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
