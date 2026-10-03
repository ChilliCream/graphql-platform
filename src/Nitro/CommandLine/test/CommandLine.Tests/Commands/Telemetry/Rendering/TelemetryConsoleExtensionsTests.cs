using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Rendering;

public sealed class TelemetryConsoleExtensionsTests
{
    [Fact]
    public void WriteListEnvelope_Should_IncludeTotalInHint_When_TotalIsProvided()
    {
        // arrange
        var (console, output) = CreateConsole();

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
    public void WriteListEnvelope_Should_AdvertiseOnlyProvidedOptions_When_ResultHasMoreItems()
    {
        // arrange
        var (console, output) = CreateConsole();

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
        var (console, output) = CreateConsole();

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
    public void WriteListEnvelope_Should_WriteEmptyEnvelope_When_ThereAreNoItems()
    {
        // arrange
        var (console, output) = CreateConsole();

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

    private static (INitroConsole Console, StringWriter Output) CreateConsole()
    {
        var output = new StringWriter();
        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(output);
        outConsole.Profile.Width = Constants.DefaultPrintWidth;

        return (new NitroConsole(outConsole, new TestConsole(), new SnapshotActivitySinkFactory()), output);
    }

    internal sealed record Sample(string Id, string Name);
}
