using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class OpencodeHooksCommandTests(NitroCommandFixture fixture) : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task ExecuteCommandAsync_Should_ExplainFailOpenLocalOnlySetup_When_OpencodeHelpIsRequested()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("fail-open", result.StdOut, StringComparison.Ordinal);
        Assert.Contains(".gitignore", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_DescribeTwoScopes_When_OpencodeInstallHelpIsRequested()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("--scope <project|user>", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("[default: user]", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_RoundTripWithoutUsingHomeDirectory_When_ProjectScopeIsUsed()
    {
        // arrange
        var sidecarDirectory = Path.Combine(WorkingDirectory, "..", "app-data");
        SetupGlobalConfigDirectory(sidecarDirectory);
        await InitWorkspaceAsync();

        // act
        var install = await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");

        // assert
        Assert.Equal(0, install.ExitCode);
        Assert.True(File.Exists(Path.Combine(WorkingDirectory, ".opencode", "plugin", "nitro-hooks.js")));

        // act
        var statusAfterInstall =
            await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        Assert.Equal(0, statusAfterInstall.ExitCode);

        // act
        var uninstall = await ExecuteCommandAsync("agent", "hooks", "opencode", "uninstall", "--scope", "project");

        // assert
        Assert.Equal(0, uninstall.ExitCode);

        // act
        var statusAfterUninstall =
            await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        Assert.Equal(1, statusAfterUninstall.ExitCode);
    }

    [Fact]
    public async Task ResolveAsync_Should_ParseVersion_When_VersionReaderReturnsVersion()
    {
        // arrange
        var resolver = new OpencodeVersionResolver(_ => Task.FromResult<string?>("opencode 1.18.23"));

        // act
        var result = await resolver.ResolveAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new Version(1, 18, 23), result);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_StateEndpointNoteUpfront_When_Installed()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "Nitro can only push to opencode when it binds an HTTP server", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("--port, --hostname, or --mdns", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_ReportSessionAsReachable_When_LastPingSucceeded()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        await InsertAgentRowAsync(
            "maya",
            "session-reachable",
            endpointKind: AgentSessionEndpointKind.OpencodeServer,
            endpointAddr: "http://127.0.0.1:51000",
            lastPingResult: AgentPingResult.Ok);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        Assert.Contains("session-reachable", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("opencode-server http://127.0.0.1:51000", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("; reachable at last ping;", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("last ping accepted", result.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("Pushes will not arrive", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_NotNameRemedy_When_BoundEndpointLastPingFailed()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        await InsertAgentRowAsync(
            "maya",
            "session-bound-ping-failed",
            endpointKind: AgentSessionEndpointKind.OpencodeServer,
            endpointAddr: "http://127.0.0.1:51000",
            lastPingResult: AgentPingResult.Error,
            lastPingDetail: "connection reset");

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        Assert.Contains("session-bound-ping-failed", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("last ping: error", result.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("Pushes will not arrive", result.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("--port", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_NameRemedy_When_EndpointIsUnreachablePlaceholder()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        await InsertAgentRowAsync("maya", "session-unreachable");

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        Assert.Contains("session-unreachable", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("no endpoint registered", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("unreachable", result.StdOut, StringComparison.Ordinal);
        Assert.Contains("never pinged", result.StdOut, StringComparison.Ordinal);
        Assert.Contains(
            "Pushes will not arrive for this session: start opencode with an explicit "
            + "--port, --hostname, or --mdns flag.",
            result.StdOut,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_NameEndpointGoneWithoutClaimingUnreachable_When_LastPingWasEndpointGone()
    {
        // arrange
        // Seed an EndpointGone ping result without detail.
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        await InsertAgentRowAsync(
            "maya",
            "session-endpoint-gone",
            endpointKind: AgentSessionEndpointKind.OpencodeServer,
            endpointAddr: "http://127.0.0.1:51000",
            lastPingResult: AgentPingResult.EndpointGone);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        Assert.Contains(
            "; endpoint gone at last ping; last ping: endpoint gone",
            result.StdOut,
            StringComparison.Ordinal);
        Assert.DoesNotContain("last ping: endpoint gone (", result.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("unreachable at last ping", result.StdOut, StringComparison.Ordinal);
        Assert.DoesNotContain("Pushes will not arrive", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_DescribeEndpointReporting_When_StatusHelpIsRequested()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--help");

        // assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("push endpoint and last ping", result.StdOut, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_IncludeEndpointNoteField_When_InstallJsonOutputIsRequested()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        Assert.Equal(
            OpencodeEndpointGuidance.InstallNote,
            document.RootElement.GetProperty("endpointNote").GetString());
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_IncludeSessionFields_When_StatusJsonOutputIsRequested()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        await InsertAgentRowAsync(
            "maya",
            "session-json",
            endpointKind: AgentSessionEndpointKind.OpencodeServer,
            endpointAddr: "http://127.0.0.1:51000",
            lastPingResult: AgentPingResult.Ok,
            lastPingDetail: "healthy");
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var session = Assert.Single(document.RootElement.GetProperty("sessions").EnumerateArray());
        Assert.Equal("session-json", session.GetProperty("sessionId").GetString());
        Assert.Equal("reachable at last ping", session.GetProperty("reachability").GetString());
        Assert.Equal(AgentPingResult.Ok, session.GetProperty("lastPingResult").GetString());
        Assert.Equal("healthy", session.GetProperty("lastPingDetail").GetString());
    }

    [Fact]
    public async Task ExecuteCommandAsync_Should_OmitTheSession_When_TheSessionHasEnded()
    {
        // arrange
        SetupGlobalConfigDirectory(Path.Combine(WorkingDirectory, "..", "app-data"));
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "hooks", "opencode", "install", "--scope", "project");
        await InsertAgentRowAsync(
            "maya",
            "session-ended",
            endpointKind: AgentSessionEndpointKind.OpencodeServer,
            endpointAddr: "http://127.0.0.1:51000",
            lastPingResult: AgentPingResult.Ok,
            endedAt: FakeTime.GetUtcNow());
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "hooks", "opencode", "status", "--scope", "project");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        document.RootElement.GetProperty("sessions").GetRawText().MatchInlineSnapshot("[]");
    }

    /// <summary>
    /// Inserts one opencode agent row directly into the unified <c>agents</c> table with
    /// fresh timestamps and the supplied endpoint and ping state.
    /// </summary>
    private async Task InsertAgentRowAsync(
        string name,
        string sessionId,
        string endpointKind = AgentSessionEndpointKind.None,
        string endpointAddr = "",
        string? lastPingResult = null,
        string? lastPingDetail = null,
        DateTimeOffset? endedAt = null)
    {
        var now = FakeTime.GetUtcNow();

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agents (
                name, harness, session_id, endpoint_kind, endpoint_addr,
                last_ping_result, last_ping_detail, registered_at, started_at, last_seen_at, ended_at
            ) VALUES (
                $name, $harness, $sessionId, $endpointKind, $endpointAddr,
                $lastPingResult, $lastPingDetail, $now, $now, $now, $endedAt
            );
            """;
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$harness", AgentSessionHarness.Opencode);
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$endpointKind", endpointKind);
        command.Parameters.AddWithValue("$endpointAddr", endpointAddr);
        command.Parameters.AddWithValue("$lastPingResult", (object?)lastPingResult ?? DBNull.Value);
        command.Parameters.AddWithValue("$lastPingDetail", (object?)lastPingDetail ?? DBNull.Value);
        command.Parameters.AddWithValue("$now", now);
        command.Parameters.AddWithValue("$endedAt", (object?)endedAt ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
