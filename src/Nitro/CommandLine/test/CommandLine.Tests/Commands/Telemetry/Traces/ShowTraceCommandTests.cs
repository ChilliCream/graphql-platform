using System.Text.Json;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Telemetry;
using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ChilliCream.Nitro.CommandLine.Tests.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using System.CommandLine;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces;

public sealed class ShowTraceCommandTests
{
    private const string WorkspaceId = "workspace";
    private static readonly DateTimeOffset s_testNow =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_Should_RenderSameAsciiTree_When_InteractionModeChanges(bool agentMode)
    {
        // arrange
        var trace = CreateTrace();
        var client = CreateClient(trace);

        // act
        var result = await ExecuteAsync(client, agentMode: agentMode);

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("trace trace-id: 2 spans (1 errors), total 25 ms", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("root [SERVER · orders · 20ms · root]", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("└─ child [SERVER · orders · 5ms · ERROR · child]", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execute_Should_PassHiddenSeekerAndSpan_When_OptionsAreSpecified()
    {
        // arrange
        var client = CreateClient(CreateTrace());

        // act
        var result = await ExecuteAsync(
            client,
            commandArguments: [
                "--span",
                "child",
                "--seeker",
                "opaque-cursor",
                "--since",
                "2h"]);

        // assert
        Assert.Equal(0, result.ExitCode);
        client.Verify(
            x => x.GetTraceAsync(
                "workspace",
                "trace-id",
                "child",
                "opaque-cursor",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("--since", "10m")]
    [InlineData("--until", "5m")]
    [InlineData("--since", "2h", "--until", "1h")]
    public async Task Execute_Should_NotDeriveSeekerFromTimeBounds(
        string firstOption,
        string firstValue,
        string? secondOption = null,
        string? secondValue = null)
    {
        // arrange
        var client = CreateClient(CreateTrace());
        var arguments = new List<string> { firstOption, firstValue };
        if (secondOption is not null && secondValue is not null)
        {
            arguments.AddRange([secondOption, secondValue]);
        }

        // act
        var result = await ExecuteAsync(client, commandArguments: arguments.ToArray());

        // assert
        Assert.Equal(0, result.ExitCode);
        client.Verify(
            x => x.GetTraceAsync(
                WorkspaceId,
                "trace-id",
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Execute_Should_RenderJson_When_OutputIsJson()
    {
        // arrange
        var longName = new string('x', 121);
        var trace = new Trace(
            1,
            false,
            12.5,
            [CreateSpan("span-id", name: longName)]);
        var client = CreateClient(trace);

        // act
        var result = await ExecuteAsync(
            client,
            commandArguments: ["--output", "json"]);

        // assert
        using var document = JsonDocument.Parse(result.StdOut);
        Assert.Equal("trace-id", document.RootElement.GetProperty("traceId").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("spanCount").GetInt32());
        Assert.Equal(
            longName,
            document.RootElement.GetProperty("spans")[0].GetProperty("spanName").GetString());
    }

    [Fact]
    public async Task Execute_Should_RenderHintAndErrorCode_When_TraceIsMissing()
    {
        // arrange
        var client = CreateClient(null);

        // act
        var result = await ExecuteAsync(client);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "The trace 'trace-id' was not found.\nhint: run nitro telemetry traces list --since 2h",
            result.StdErr);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Execute_Should_RenderNotFound_When_TraceHasNoSpans(
        bool includeSpan,
        bool json)
    {
        // arrange
        var client = CreateClient(new Trace(0, false, 0, []));
        var arguments = new List<string>();
        if (includeSpan)
        {
            arguments.AddRange(["--span", "span-id"]);
        }

        if (json)
        {
            arguments.AddRange(["--output", "json"]);
        }

        // act
        var result = await ExecuteAsync(client, commandArguments: arguments.ToArray());

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            "The trace 'trace-id' was not found.\nhint: run nitro telemetry traces list --since 2h",
            result.StdErr);
        Assert.Empty(result.StdOut);
    }

    [Fact]
    public async Task Execute_Should_RejectNdjsonOutput()
    {
        // arrange
        var client = CreateClient(CreateTrace());

        // act
        var result = await ExecuteAsync(client, commandArguments: ["--output", "ndjson"]);

        // assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("does not support ndjson", result.StdErr, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execute_Should_ReportOverviewDisplayCap()
    {
        // arrange
        var spans = Enumerable.Range(0, 120)
            .Select(i => CreateSpan($"span-{i}"))
            .ToArray();
        var client = CreateClient(new Trace(120, false, 120, spans));

        // act
        var result = await ExecuteAsync(client);

        // assert
        Assert.Contains("shows 96 of 120 spans", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execute_Should_ReportFocusedDisplayCapForReturnedSubtree()
    {
        // arrange
        var spans = new List<TraceSpan> { CreateSpan("focus") };
        for (var i = 0; i < 24; i++)
        {
            var childId = $"child-{i}";
            spans.Add(CreateSpan(childId, "focus"));
            spans.Add(CreateSpan($"leaf-{i}", childId));
        }

        spans.Add(CreateSpan("leaf-extra", "child-0"));
        spans.Add(CreateSpan("outside"));
        var client = CreateClient(new Trace(51, false, 51, spans));

        // act
        var result = await ExecuteAsync(client, commandArguments: ["--span", "focus"]);

        // assert
        Assert.Contains("shows 40 of 50 spans", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execute_Should_LabelServerCappedSummaryWhenCountIsUnknown()
    {
        // arrange
        var client = CreateClient(new Trace(null, true, 0, [CreateSpan("root", duration: 20)]));

        // act
        var result = await ExecuteAsync(client);

        // assert
        Assert.Equal(
            "trace trace-id: 1 returned spans (errors unknown), total span count unknown, "
            + "duration unknown (server-capped)",
            result.StdOut.Split(Environment.NewLine)[0]);
    }

    private static Mock<ITelemetryClient> CreateClient(Trace? trace)
    {
        var client = new Mock<ITelemetryClient>(MockBehavior.Strict);
        client
            .Setup(x => x.GetTraceAsync(
                "workspace",
                "trace-id",
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trace);
        return client;
    }

    private static async Task<CommandResult> ExecuteAsync(
        Mock<ITelemetryClient> client,
        bool agentMode = false,
        params string[] commandArguments)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var outConsole = new TestConsole();
        outConsole.Profile.Out = new AnsiConsoleOutput(output);
        outConsole.Profile.Width = Constants.DefaultPrintWidth;
        outConsole.Profile.Capabilities.Interactive = !agentMode;
        var errorConsole = new TestConsole();
        errorConsole.Profile.Out = new AnsiConsoleOutput(error);
        var console = new NitroConsole(
            outConsole,
            errorConsole,
            new SnapshotActivitySinkFactory(),
            agentMode);
        var environment = new Mock<IEnvironmentVariableProvider>();
        environment
            .Setup(x => x.GetEnvironmentVariable(It.IsAny<string>()))
            .Returns((string?)null);
        var session = new Mock<ISessionService>();
        var context = new NitroClientContext();
        context.Configure(null, null);
        var services = new ServiceCollection();
        services.AddSingleton<INitroConsole>(console);
        services.AddSingleton<ITelemetryClient>(client.Object);
        services.AddSingleton<ISessionService>(session.Object);
        services.AddSingleton<IEnvironmentVariableProvider>(environment.Object);
        services.AddSingleton<TimeProvider>(new FakeTimeProvider(s_testNow));
        services.AddSingleton(context);
        services.AddSingleton<INitroClientContextProvider>(context);
        var provider = services.BuildServiceProvider();
        CommandExecutionContext.Initialize(new CommandServices(provider));

        var root = new RootCommand();
        var traces = new Command("traces");
        traces.Subcommands.Add(new ShowTraceCommand());
        root.Subcommands.Add(traces);
        var arguments = new List<string> { "traces", "show", "trace-id", "--api-key", "key", "--workspace-id", "workspace" };
        arguments.AddRange(commandArguments);
        var parseResult = root.Parse(arguments.ToArray());
        var exitCode = await parseResult.InvokeAsync(
            new InvocationConfiguration
            {
                Output = output,
                Error = error
            });

        return new CommandResult(
            exitCode,
            output.ToString().TrimEnd(),
            error.ToString().TrimEnd(),
            root.Name);
    }

    private static Trace CreateTrace()
        => new(
            2,
            false,
            25,
            [
                CreateSpan(
                    "root",
                    duration: 20,
                    resourceAttributes: [new("service.name", "orders")]),
                CreateSpan(
                    "child",
                    parent: "root",
                    duration: 5,
                    status: "ERROR",
                    resourceAttributes: [new("service.name", "orders")])
            ]);

    private static TraceSpan CreateSpan(
        string id,
        string parent = "",
        string? name = null,
        double duration = 1,
        string status = "OK",
        IReadOnlyList<TelemetryAttribute>? resourceAttributes = null)
        => new(
            id,
            parent,
            name ?? id,
            "SERVER",
            duration,
            0,
            status,
            string.Empty,
            resourceAttributes ?? [],
            [],
            [],
            null);
}
