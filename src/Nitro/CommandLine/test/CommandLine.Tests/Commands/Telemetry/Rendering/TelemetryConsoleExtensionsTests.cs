using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Rendering;

public sealed class TelemetryConsoleExtensionsTests
{
    [Fact]
    public void WriteListEnvelope_Should_WriteEnvelope_When_ConsoleIsNonInteractive()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        console.Profile.Capabilities.Interactive = false;

        // act
        console.WriteListEnvelope(
            [new Sample("first", "First"), new Sample("second", "Second")],
            total: 3,
            hasMore: true,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            [
                Opt<TelemetrySinceOption>.Instance,
                Opt<TelemetryServiceOption>.Instance,
                Opt<TelemetryFilterOption>.Instance
            ]);

        // assert
        output
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                {
                  "items": [
                    {
                      "id": "first",
                      "name": "First"
                    },
                    {
                      "id": "second",
                      "name": "Second"
                    }
                  ],
                  "returned": 2,
                  "total": 3,
                  "hasMore": true,
                  "hint": "showing 2 of 3 (more), narrow with --since, --service or --filter, or raise --limit"
                }
                """);
    }

    [Fact]
    public void WriteListEnvelope_Should_WriteEnvelope_When_OutputIsJson()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        console.SetOutputFormat(OutputFormat.Json);
        var item = new Sample("first", new string('a', 121));

        // act
        console.WriteListEnvelope(
            [item],
            total: 1,
            hasMore: false,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            []);

        // assert
        using var document = JsonDocument.Parse(output.ToString());
        Assert.Equal(item.Name, document.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void WriteListEnvelope_Should_WriteEnvelope_When_InteractiveOutputHasMoreItems()
    {
        // arrange
        var (console, output, _) = CreateConsole();

        // act
        console.WriteListEnvelope(
            [new Sample("first", "First")],
            total: null,
            hasMore: true,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            [
                Opt<TelemetrySinceOption>.Instance,
                Opt<TelemetryServiceOption>.Instance,
                Opt<TelemetryFilterOption>.Instance
            ]);

        // assert
        output
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                {
                  "items": [
                    {
                      "id": "first",
                      "name": "First"
                    }
                  ],
                  "returned": 1,
                  "total": null,
                  "hasMore": true,
                  "hint": "showing 1 (more), narrow with --since, --service or --filter, or raise --limit"
                }
                """);
    }

    [Fact]
    public void WriteListEnvelope_Should_AdvertiseOnlyProvidedOptions_When_ResultHasMoreItems()
    {
        // arrange
        var (console, output, _) = CreateConsole();

        // act
        console.WriteListEnvelope(
            [new Sample("first", "First")],
            total: null,
            hasMore: true,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            [Opt<TelemetrySinceOption>.Instance, Opt<TelemetryFilterOption>.Instance]);

        // assert
        output
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                {
                  "items": [
                    {
                      "id": "first",
                      "name": "First"
                    }
                  ],
                  "returned": 1,
                  "total": null,
                  "hasMore": true,
                  "hint": "showing 1 (more), narrow with --since or --filter, or raise --limit"
                }
                """);
    }

    [Fact]
    public void WriteListEnvelope_Should_AdvertiseSingleOption_When_OnlyOneOptionIsProvided()
    {
        // arrange
        var (console, output, _) = CreateConsole();

        // act
        console.WriteListEnvelope(
            [new Sample("first", "First")],
            total: null,
            hasMore: true,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            [Opt<TelemetrySinceOption>.Instance]);

        // assert
        output
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                {
                  "items": [
                    {
                      "id": "first",
                      "name": "First"
                    }
                  ],
                  "returned": 1,
                  "total": null,
                  "hasMore": true,
                  "hint": "showing 1 (more), narrow with --since, or raise --limit"
                }
                """);
    }

    [Fact]
    public void WriteListEnvelope_Should_WriteEmptyEnvelope_When_NonInteractiveConsoleHasNoItems()
    {
        // arrange
        var (console, output, _) = CreateConsole();
        console.Profile.Capabilities.Interactive = false;

        // act
        console.WriteListEnvelope(
            Array.Empty<Sample>(),
            total: 0,
            hasMore: false,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            []);

        // assert
        output
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                {
                  "items": [],
                  "returned": 0,
                  "total": 0,
                  "hasMore": false
                }
                """);
    }

    [Fact]
    public void WriteListEnvelope_Should_WriteEmptyEnvelope_When_InteractiveModeHasNoItems()
    {
        // arrange
        var (console, output, _) = CreateConsole();

        // act
        console.WriteListEnvelope(
            Array.Empty<Sample>(),
            total: 0,
            hasMore: false,
            TelemetryConsoleExtensionsJsonContext.Default.Sample,
            emptyResultHint: null,
            []);

        // assert
        output
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                {
                  "items": [],
                  "returned": 0,
                  "total": 0,
                  "hasMore": false
                }
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
        error
            .ToString()
            .TrimEnd()
            .MatchInlineSnapshot(
                """
                The filter is invalid.
                hint: use key:value
                """);
        Assert.Equal(ExitCodes.Error, exitCode);
    }

    private static (INitroConsole Console, StringWriter Output, StringWriter Error) CreateConsole()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(output);
        outConsole.Profile.Width = Constants.DefaultPrintWidth;
        outConsole.Profile.Capabilities.Interactive = true;
        var errorConsole = new TestConsole();
        errorConsole.Profile.Out = new AnsiConsoleOutput(error);

        return (new NitroConsole(outConsole, errorConsole, new SnapshotActivitySinkFactory()), output, error);
    }

    internal sealed record Sample(string Id, string Name);
}
