using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed class PolicyRequirementsTests
{
    [Fact]
    public void Empty_Should_ReadNoParts()
    {
        // arrange & act
        var requirements = PolicyRequirements.Empty;

        // assert
        var report = $"""
            resource: {requirements.Resource?.ToString(false) ?? "<null>"}
            cacheable: {requirements.IsRequestCacheable}
            """;
        report.MatchInlineSnapshot(
            """
            resource: <null>
            cacheable: True
            """);
    }

    [Fact]
    public void IsRequestCacheable_Should_MatchTruthTable()
    {
        // arrange
        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");

        // act
        var table = string.Join(
            Environment.NewLine,
            Row("empty", PolicyRequirements.Empty),
            Row("resource", new PolicyRequirements { Resource = selectionSet }));

        // assert
        table.MatchInlineSnapshot(
            """
            empty: True
            resource: False
            """);
    }

    private static string Row(string name, PolicyRequirements requirements)
        => $"{name}: {requirements.IsRequestCacheable}";
}
