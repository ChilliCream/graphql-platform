using System.Text.Json;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Packaging;

namespace HotChocolate.Fusion;

public sealed class GatewaySettingsOverlayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComposeAsync_Should_PreserveGatewaySettings_When_Recomposing(bool useOverlay)
    {
        // arrange
        using var sourceSettings = JsonDocument.Parse("""{ "name": "Products" }""");
        using var initialSettings = JsonDocument.Parse(
            """{ "cost": { "maxFieldCost": 10, "maxTypeCost": 20, "nested": { "first": 1, "second": 2 } }, "planner": { "enabled": true } }""");
        using var overlay = JsonDocument.Parse(
            """{ "cost": { "maxFieldCost": 30, "nested": { "first": 3 } }, "extra": [1, 2] }""");
        var sources = new Dictionary<string, LocalSourceSchema>
        {
            ["Products"] = new(
                new SourceSchemaText("Products", "type Query { product: String }"),
                sourceSettings,
                urlOverride: null)
        };
        await using var stream = new MemoryStream();
        using (var archive = FusionArchive.Create(stream, leaveOpen: true))
        {
            var initial = await CompositionHelper.ComposeAsync(
                new CompositionLog(), sources, archive, "Development", false, null,
                initialSettings, null, TestContext.Current.CancellationToken);
            Assert.True(initial.IsSuccess);
        }

        stream.Position = 0;
        using var reviewsSettings = JsonDocument.Parse("""{ "name": "Reviews" }""");
        var reviews = new Dictionary<string, LocalSourceSchema>
        {
            ["Reviews"] = new(
                new SourceSchemaText("Reviews", "type Query { review: String }"),
                reviewsSettings,
                urlOverride: null)
        };

        // act
        using (var archive = FusionArchive.Open(stream, FusionArchiveMode.Update, leaveOpen: true))
        {
            var result = await CompositionHelper.ComposeAsync(
                new CompositionLog(), reviews, archive,
                "Development", false, null, useOverlay ? overlay : null, null,
                TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess);
        }

        // assert
        stream.Position = 0;
        using var reader = FusionArchive.Open(stream, leaveOpen: true);
        using var config = await reader.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion, TestContext.Current.CancellationToken);
        var snapshot = Snapshot.Create(postFix: useOverlay ? "Overlay" : "Preserved");
        snapshot.Add(config!.Settings.RootElement, "settings");
        snapshot.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{ \"sourceSchemas\": {} }")]
    public async Task ComposeAsync_Should_RejectInvalidGatewaySettings_When_OverlayIsInvalid(string json)
    {
        // arrange
        using var settings = JsonDocument.Parse(json);
        await using var stream = new MemoryStream();
        using var archive = FusionArchive.Create(stream, leaveOpen: true);
        var log = new CompositionLog();

        // act
        var result = await CompositionHelper.ComposeAsync(
            log, new Dictionary<string, LocalSourceSchema>(), archive, "Development", false,
            null, settings, null, TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.IsFailure);
        Assert.Collection(log, entry =>
        {
            Assert.Equal(LogEntryCodes.InvalidGatewaySettings, entry.Code);
            Assert.Equal(LogSeverity.Error, entry.Severity);
        });
    }
}
