using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies every vendored conformance fixture is well-formed before the
/// engine exists to evaluate it: its schema and operation parse, its
/// variables shape a JSON object or null, and its expected numbers are
/// finite. The evaluate theory that checks the expected numbers against the
/// compiled engine lands with core-plan-evaluate.
/// </summary>
public sealed class FixtureWellFormednessTests
{
    public static TheoryData<string> FixturePaths => FixtureLoader.DiscoverFixturePaths();

    [Theory]
    [MemberData(nameof(FixturePaths))]
    public void Fixture_Should_BeWellFormed_When_Loaded(string path)
    {
        // arrange
        var fixture = Fixture.Load(path);

        // act
        var schema = SchemaParser.Parse(fixture.Sdl);
        var operation = Utf8GraphQLParser.Parse(fixture.Operation)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .SingleOrDefault(definition => definition.Name?.Value == fixture.OperationName);

        // assert
        Assert.NotNull(schema.QueryType);
        Assert.NotNull(operation);
        Assert.True(fixture.Variables is null || fixture.Variables.Value.ValueKind == JsonValueKind.Object);
        Assert.True(double.IsFinite(fixture.Expected.TypeCost) && double.IsFinite(fixture.Expected.FieldCost));
    }
}
