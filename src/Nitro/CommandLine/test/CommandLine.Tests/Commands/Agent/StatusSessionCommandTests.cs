using System.Diagnostics;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class StatusSessionCommandTests(NitroCommandFixture fixture)
    : AgentCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("agent", "session", "status", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Show the live harness sessions claimed by the resolved actor.

            Usage:
              nitro agent session status [options]

            Options:
              --actor <actor>  The acting identity used on mail commands (defaults to NITRO_MAIL_ACTOR, NITRO_TASK_ACTOR, or the OS user name)
              --output <json>  The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help   Show help and usage information

            Example:
              nitro agent session status
              nitro agent session status --actor codex
            """);
    }

    [Fact]
    public async Task NoLiveSession_PrintsOffline()
    {
        // arrange
        await InitWorkspaceAsync();

        // act
        var result = await ExecuteCommandAsync("agent", "session", "status");

        // assert
        result.AssertSuccess(
            """
            test-agent  offline
            """);
    }

    [Fact]
    public async Task ClaimedSession_PrintsItsState()
    {
        // arrange
        await InitWorkspaceAsync();
        await ExecuteCommandAsync("agent", "register");
        var host = await ResolveThisMachinesInstanceIdAsync();
        await InsertAliveSessionRowAsync(host, "session-1", "test-agent");

        // act
        var result = await ExecuteCommandAsync("agent", "session", "status");

        // assert
        var line = Assert.Single(result.StdOut.Split('\n'));
        Assert.Contains("test-agent", line);
        Assert.Contains("session-1", line);
    }

    [Fact]
    public async Task JsonOutput_NoLiveSession_ReturnsOfflineFalse()
    {
        // arrange
        await InitWorkspaceAsync();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync("agent", "session", "status");

        // assert
        using var document = System.Text.Json.JsonDocument.Parse(result.StdOut);
        var root = document.RootElement;

        Assert.Equal("test-agent", root.GetProperty("actor").GetString());
        Assert.False(root.GetProperty("online").GetBoolean());
        Assert.Equal(0, root.GetProperty("sessions").GetArrayLength());
    }

    [Fact]
    public async Task NoWorkspace_ReturnsError()
    {
        // act
        var result = await ExecuteCommandAsync("agent", "session", "status");

        // assert
        result.AssertError(
            """
            No agent workspace found. Run `nitro agent init` first.
            """);
    }

    private static async Task<string> ResolveThisMachinesInstanceIdAsync()
    {
        var provider = new NitroInstanceIdProvider(new FileSystem());
        var directory = new GlobalConfigDirectoryProvider().GetDirectory();

        return await provider.GetIdAsync(directory, TestContext.Current.CancellationToken);
    }

    private async Task InsertAliveSessionRowAsync(string host, string sessionId, string agentName)
    {
        using var process = Process.GetCurrentProcess();
        var pid = process.Id;
        var procStart = process.StartTime.ToUniversalTime();

        await using var connection = new SqliteConnection($"Data Source={DatabasePath};Pooling=False");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO agent_sessions (
                harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
            ) VALUES (
                'claude-code', $sessionId, $agentName, 'explicit', $host, $pid, $procStart,
                '/work', '/work/.nitro/agents', 'none', '', $now, $now
            );
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$agentName", agentName);
        command.Parameters.AddWithValue("$host", host);
        command.Parameters.AddWithValue("$pid", pid);
        command.Parameters.AddWithValue("$procStart", procStart);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
