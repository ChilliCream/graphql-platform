using System.CommandLine;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Options;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Rendering;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Spectre.Console;
using Spectre.Console.Testing;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Rendering;

public sealed class TelemetryOutputFormatRendererTests
{
    [Fact]
    public async Task Execute_Should_WriteNdjson_When_OutputFormatIsNdjson()
    {
        // arrange
        var (console, output) = CreateConsole();
        var renderer = new TelemetryListRenderer(console);
        var outputFormat = new TelemetryOutputFormatOption();
        var command = new Command("telemetry");
        OutputFormat? receivedOutputFormat = null;
        command.Options.Add(outputFormat);
        command.SetAction((parseResult, _) =>
        {
            receivedOutputFormat = parseResult.GetValue(outputFormat);
            console.SetOutputFormat(receivedOutputFormat!.Value);
            renderer.Render(
                [new Sample("first", "First"), new Sample("second", "Second")],
                total: 3,
                hasMore: true,
                things: "traces",
                TelemetryOutputFormatRendererJsonContext.Default.Sample,
                new TelemetryListColumn<Sample>("Id", item => item.Id));

            return Task.FromResult(ExitCodes.Success);
        });

        // act
        var exitCode = await command.Parse(["--output", "ndjson"]).InvokeAsync(
            new InvocationConfiguration
            {
                Output = TextWriter.Null,
                Error = TextWriter.Null
            },
            CancellationToken.None);

        // assert
        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(OutputFormat.Ndjson, receivedOutputFormat);
        output.ToString().TrimEnd().MatchInlineSnapshot(
            """
            {"id":"first","name":"First"}
            {"id":"second","name":"Second"}
            """);
    }

    [Fact]
    public async Task Execute_Should_WriteEnvelope_When_OutputFormatIsJson()
    {
        // arrange
        var (console, output) = CreateConsole();
        var renderer = new TelemetryListRenderer(console);
        var outputFormat = new TelemetryOutputFormatOption();
        var command = new Command("telemetry");
        OutputFormat? receivedOutputFormat = null;
        command.Options.Add(outputFormat);
        command.SetAction((parseResult, _) =>
        {
            receivedOutputFormat = parseResult.GetValue(outputFormat);
            console.SetOutputFormat(receivedOutputFormat!.Value);
            renderer.Render(
                [new Sample("first", "First")],
                total: 1,
                hasMore: false,
                things: "traces",
                TelemetryOutputFormatRendererJsonContext.Default.Sample,
                new TelemetryListColumn<Sample>("Id", item => item.Id));

            return Task.FromResult(ExitCodes.Success);
        });

        // act
        var exitCode = await command.Parse(["--output", "json"]).InvokeAsync(
            new InvocationConfiguration
            {
                Output = TextWriter.Null,
                Error = TextWriter.Null
            },
            CancellationToken.None);

        // assert
        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Equal(OutputFormat.Json, receivedOutputFormat);
        output.ToString().TrimEnd().MatchInlineSnapshot(
            """
            {"items":[
            {"id":"first","name":"First"}
            ],"returned":1,"total":1,"hasMore":false}
            """);
    }

    private static (INitroConsole Console, StringWriter Output) CreateConsole()
    {
        var output = new StringWriter();
        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(output);

        return (
            new NitroConsole(
                outConsole,
                new TestConsole(),
                new SnapshotActivitySinkFactory()),
            output);
    }

    internal sealed record Sample(string Id, string Name);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TelemetryOutputFormatRendererTests.Sample))]
internal partial class TelemetryOutputFormatRendererJsonContext : JsonSerializerContext;
