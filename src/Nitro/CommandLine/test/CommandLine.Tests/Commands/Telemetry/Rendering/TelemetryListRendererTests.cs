using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Rendering;

public sealed class TelemetryListRendererTests
{
    [Fact]
    public void Render_Should_WriteEnvelope_When_AgentModeIsEnabled()
    {
        // arrange
        var (console, output, _) = CreateConsole(isAgentMode: true);
        var renderer = new TelemetryListRenderer(console);

        // act
        renderer.Render(
            [new Sample("first", "First"), new Sample("second", "Second")],
            total: 3,
            hasMore: true,
            things: "traces",
            TelemetryListRendererJsonContext.Default.Sample,
            new TelemetryListColumn<Sample>("Id", item => item.Id),
            new TelemetryListColumn<Sample>("Name", item => item.Name));

        // assert
        output.ToString().TrimEnd().MatchInlineSnapshot(
            """
            {"items":[
            {"id":"first","name":"First"},
            {"id":"second","name":"Second"}
            ],"returned":2,"total":3,"hasMore":true,"hint":"showing 2 of 3 (more), narrow with --since, --service or --filter, or raise --limit"}
            """);
    }

    [Fact]
    public void Render_Should_WriteEnvelope_When_OutputIsJson()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        console.SetOutputFormat(OutputFormat.Json);
        var renderer = new TelemetryListRenderer(console);
        var item = new Sample("first", new string('a', 121));

        // act
        renderer.Render(
            [item],
            total: 1,
            hasMore: false,
            things: "traces",
            TelemetryListRendererJsonContext.Default.Sample,
            new TelemetryListColumn<Sample>("Name", value => value.Name));

        // assert
        using var document = JsonDocument.Parse(output.ToString());
        Assert.Equal(item.Name, document.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Truncate_Should_UseEllipsis_When_TableValueExceedsMaximumLength()
    {
        // arrange
        var value = new string('a', 121);

        // act
        var truncated = TelemetryListRenderer.Truncate(value);

        // assert
        truncated.MatchInlineSnapshot(
            """
            aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa…
            """);
    }

    [Fact]
    public void Render_Should_TruncateValues_When_RenderingTable()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        var renderer = new TelemetryListRenderer(console);
        var name = new string('a', 121);

        // act
        renderer.Render(
            [new Sample("first", name)],
            total: 1,
            hasMore: false,
            things: "traces",
            TelemetryListRendererJsonContext.Default.Sample,
            new TelemetryListColumn<Sample>("Name", item => item.Name));

        // assert
        var row = Assert.Single(
            output
                .ToString()
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries),
            line => line.Contains('…'));
        Assert.Equal($"│ {TelemetryListRenderer.Truncate(name)} │", row);
    }

    [Fact]
    public void Render_Should_WriteHintLine_When_TtyOutputHasMoreItems()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        var renderer = new TelemetryListRenderer(console);

        // act
        renderer.Render(
            [new Sample("first", "First")],
            total: null,
            hasMore: true,
            things: "traces",
            TelemetryListRendererJsonContext.Default.Sample,
            new TelemetryListColumn<Sample>("Id", item => item.Id));

        // assert
        var lines = output
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[^1].MatchInlineSnapshot(
            """
            showing 1 (more), narrow with --since, --service or --filter, or raise --limit
            """);
    }

    [Fact]
    public void Render_Should_WriteEmptyEnvelope_When_AgentModeHasNoItems()
    {
        // arrange
        var (console, output, _) = CreateConsole(isAgentMode: true);
        var renderer = new TelemetryListRenderer(console);

        // act
        renderer.Render(
            Array.Empty<Sample>(),
            total: 0,
            hasMore: false,
            things: "traces",
            TelemetryListRendererJsonContext.Default.Sample,
            new TelemetryListColumn<Sample>("Id", item => item.Id));

        // assert
        output.ToString().TrimEnd().MatchInlineSnapshot(
            """
            {"items":[],"returned":0,"total":0,"hasMore":false}
            """);
    }

    [Fact]
    public void Render_Should_WriteEmptyMessage_When_TtyHasNoItems()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        var renderer = new TelemetryListRenderer(console);

        // act
        renderer.Render(
            Array.Empty<Sample>(),
            total: 0,
            hasMore: false,
            things: "traces",
            TelemetryListRendererJsonContext.Default.Sample,
            new TelemetryListColumn<Sample>("Id", item => item.Id));

        // assert
        output.ToString().TrimEnd().MatchInlineSnapshot(
            """
            No traces found.
            """);
    }

    [Fact]
    public void Render_Should_WriteHintToStandardError_When_RenderingError()
    {
        // arrange
        var (console, _, error) = CreateConsole();

        // act
        var exitCode = TelemetryErrorRenderer.Render(console, "The filter is invalid.", "use key:value");

        // assert
        error.ToString().TrimEnd().MatchInlineSnapshot(
            """
            The filter is invalid.
            hint: use key:value
            """);
        Assert.Equal(ExitCodes.Error, exitCode);
    }

    private static (INitroConsole Console, StringWriter Output, StringWriter Error) CreateConsole(
        bool isAgentMode = false)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(output);
        outConsole.Profile.Width = Constants.DefaultPrintWidth;
        outConsole.Profile.Capabilities.Interactive = true;
        var errorConsole = new TestConsole();
        errorConsole.Profile.Out = new AnsiConsoleOutput(error);

        return (
            new NitroConsole(
                outConsole,
                errorConsole,
                new SnapshotActivitySinkFactory(),
                isAgentMode),
            output,
            error);
    }

    internal sealed record Sample(string Id, string Name);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(TelemetryListRendererTests.Sample))]
internal partial class TelemetryListRendererJsonContext : JsonSerializerContext;
