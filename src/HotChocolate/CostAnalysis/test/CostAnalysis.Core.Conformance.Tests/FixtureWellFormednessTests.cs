using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies every vendored conformance fixture has a parseable schema and
/// operation, valid variables shape and finite expected numbers.
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
        Assert.True(double.IsFinite(fixture.Expected.TypeCost));
        Assert.True(double.IsFinite(fixture.Expected.FieldCost));
    }

    [Fact]
    public void DiscoverFixturePaths_Should_FindFixtures_When_ResourcesAreCopied()
    {
        // act
        var count = FixtureLoader.FixtureCount;

        // assert
        Assert.True(count > 0, "No conformance fixtures were discovered next to the test assembly.");
    }
}
