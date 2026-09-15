using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Runs two custom algebras built only against the public
/// <see cref="AnalysisPlan"/>/<see cref="AnalysisPlanCompiler"/> surface
/// over the Conformance.Tests precision fixtures, for a fixed variable set
/// per fixture. These fixtures assert IBM cost numbers elsewhere; here they
/// only supply schemas and operations exercising fragments, exclusive type
/// regions, duplicate response names and Boolean-gated selections, so a
/// structural algebra sees the same document shapes the cost algebra does.
/// </summary>
public sealed class PrecisionFixtureAlgebraTests
{
    // Hand-derived from each fixture's operation for the fixed variable set
    // the fixture itself supplies (see each JSON file's "variables").
    private static readonly Dictionary<string, (int FieldCount, int MaxDepth)> s_expected = new(StringComparer.Ordinal)
    {
        ["c1-exclusive-types"] = (2, 2),
        ["c1-exclusive-types-fragments"] = (2, 2),
        ["c2-list-size-variable"] = (2, 2),
        ["c3-complementary-include-skip"] = (3, 2),
        ["c4-duplicate-response-name"] = (2, 2),
        ["c4-duplicate-response-name-fragments"] = (2, 2),
        ["c5-signed-weights"] = (4, 3),
        ["c5-signed-weights-merged-control"] = (4, 3),
        ["c6-zero-length-list"] = (2, 2)
    };

    public static TheoryData<string> PrecisionFixtures => PrecisionFixtureLoader.FixturePaths();

    [Theory]
    [MemberData(nameof(PrecisionFixtures))]
    public void FieldCountAlgebra_Should_MatchHandDerivedCount_When_EvaluatedOverPrecisionFixture(string path)
    {
        // arrange
        var fixture = PrecisionFixture.Load(path);
        var (plan, variables) = Compile(fixture);
        var expected = s_expected[fixture.Id];

        // act
        var fieldCount = plan.Evaluate(new FieldCountAlgebra(), variables);

        // assert
        Assert.Equal(expected.FieldCount, fieldCount);
    }

    [Theory]
    [MemberData(nameof(PrecisionFixtures))]
    public void MaxDepthAlgebra_Should_MatchHandDerivedDepth_When_EvaluatedOverPrecisionFixture(string path)
    {
        // arrange
        var fixture = PrecisionFixture.Load(path);
        var (plan, variables) = Compile(fixture);
        var expected = s_expected[fixture.Id];

        // act
        var maxDepth = plan.Evaluate(new MaxDepthAlgebra(), variables);

        // assert
        Assert.Equal(expected.MaxDepth, maxDepth);
    }

    private static (AnalysisPlan Plan, LiteralCostVariableValues Variables) Compile(PrecisionFixture fixture)
    {
        var schema = SchemaParser.Parse(fixture.Sdl);
        var document = Utf8GraphQLParser.Parse(fixture.Operation);
        var operation = document.Definitions
            .OfType<OperationDefinitionNode>()
            .Single(definition => definition.Name?.Value == fixture.OperationName);
        var snapshot = CostSchemaSnapshot.Create(schema, new CostEngineOptions());
        var plan = AnalysisPlanCompiler.Compile(snapshot, document, operation);
        var variables = FixtureVariables.Read(fixture.Variables);

        return (plan, variables);
    }
}
