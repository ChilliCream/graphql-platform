using System.Globalization;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

public sealed class ConformanceTests
{
    public static TheoryData<string> ArticleFixtures => FixtureLoader.Family("article");

    public static TheoryData<string> RustUnitFixtures => FixtureLoader.Family("rust-unit");

    public static TheoryData<string> FuzzFoundFixtures => FixtureLoader.Family("fuzz-found");

    public static TheoryData<string> CorpusFixtures => FixtureLoader.RustCorpus();

    [Theory(SkipTestWithoutData = true)]
    [MemberData(nameof(ArticleFixtures))]
    public void Article_Fixture_Should_MatchOracle_When_Evaluated(string path)
        => AssertFixture(Fixture.Load(path));

    [Theory(SkipTestWithoutData = true)]
    [MemberData(nameof(RustUnitFixtures))]
    public void RustUnit_Fixture_Should_MatchOracle_When_Evaluated(string path)
        => AssertFixture(Fixture.Load(path));

    [Theory(SkipTestWithoutData = true)]
    [MemberData(nameof(FuzzFoundFixtures))]
    public void FuzzFound_Fixture_Should_MatchOracle_When_Evaluated(string path)
        => AssertFixture(Fixture.Load(path));

    [Theory]
    [MemberData(nameof(CorpusFixtures))]
    public void Corpus_Should_MatchOracle_When_Evaluated(string displayName)
        => AssertFixture(FixtureLoader.LoadRustCorpusFixture(displayName));

    private static void AssertFixture(Fixture fixture)
    {
        // arrange
        var schema = SchemaParser.Parse(fixture.Sdl);
        var document = Utf8GraphQLParser.Parse(fixture.Operation);
        var operation = document.Definitions
            .OfType<OperationDefinitionNode>()
            .Single(definition => definition.Name?.Value == fixture.OperationName);
        var options = new CostEngineOptions { DefaultListSize = ReadDefaultListSize(fixture.DefaultListSize) };
        var snapshot = CostSchemaSnapshot.Create(schema, options);
        var plan = CostPlanCompiler.Compile(snapshot, document, operation, CostAnalyses.Cost);

        // act
        var estimate = fixture.Variables is { } variables
            ? Evaluate(plan, fixture.Sdl, operation, variables)
            : plan.EvaluateStaticBound();

        // assert
        Assert.Equal(fixture.Expected.TypeCost, estimate.TypeCost);
        Assert.Equal(fixture.Expected.FieldCost, estimate.FieldCost);
    }

    private static CostEstimate Evaluate(
        CostPlan plan,
        string schemaSource,
        OperationDefinitionNode operation,
        System.Text.Json.JsonElement variables)
    {
        var coercionSchema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(schemaSource + "\nenum fusion__Schema { FIXTURE }"));

        if (!VariableCoercionHelper.TryCoerceVariableValues(
                new TestFeatureProvider(),
                coercionSchema,
                operation.VariableDefinitions,
                variables,
                out var coerced,
                out var error))
        {
            Assert.Fail(error.Message);
        }

        return plan.Evaluate(new CostVariableValuesAdapter(coerced));
    }

    private static double ReadDefaultListSize(System.Text.Json.JsonElement value)
        => value.ValueKind == System.Text.Json.JsonValueKind.String
            && string.Equals(value.GetString(), "Infinity", StringComparison.Ordinal)
                ? double.PositiveInfinity
                : double.Parse(value.GetRawText(), CultureInfo.InvariantCulture);

    private sealed class TestFeatureProvider : IFeatureProvider
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
    }
}
