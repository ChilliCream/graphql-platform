using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="ClaudeHookExecutor"/>'s fail-open envelope directly
/// against <see cref="StringReader"/>/<see cref="StringWriter"/>, without
/// System.CommandLine or DI: the suppression short-circuit, every failure
/// path resolving to the neutral <c>{}</c> response, and successful
/// translation of the captured Claude payload fixtures into the harness's
/// wire shape.
/// </summary>
public sealed class ClaudeHookExecutorTests
{
    [Fact]
    public async Task RunAsync_Should_WriteNeutralWithoutInvokingTheHandler_When_SuppressEnvVarIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var environmentVariables = new FixedEnvironmentVariableProvider();
        environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        var input = new StringReader(HookFixtures.Read("session-start.json"));
        var output = new StringWriter();
        var handlerInvoked = false;

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            environmentVariables,
            input,
            output,
            (_, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(ClaudeHookOutcome.Neutral);
            },
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
        Assert.False(handlerInvoked);
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_PayloadIsMalformedJson()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("malformed.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => throw new InvalidOperationException("must not be reached"),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_HandlerThrows()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => throw new InvalidOperationException("simulated database contention"),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheEntryTimeoutElapses()
    {
        // arrange: an explicit short timeout stands in for the real 10s
        // entry ceiling so this test does not have to wait it out.
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            async (_, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return ClaudeHookOutcome.Neutral; },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheHandlerIgnoresCancellation()
    {
        // arrange: the handler never observes the linked token at all (a
        // hung database call, for instance), so only racing the entry
        // timeout against the handler task - never awaiting the handler
        // task itself on timeout - can keep this call within the deadline.
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            async (_, _) => { await Task.Delay(Timeout.InfiniteTimeSpan); return ClaudeHookOutcome.Neutral; },
            TimeSpan.FromMilliseconds(50),
            cancellationToken);

        stopwatch.Stop();

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"expected RunAsync to return near the 50ms timeout, took {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheDatabaseIsContended()
    {
        // arrange: a second connection holds an open write transaction on
        // the workspace database, so the Stop handler's ledger reservation
        // write blocks waiting for the lock. The executor's short timeout
        // must still resolve to neutral instead of waiting out SQLite's own
        // (far longer) default busy timeout.
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = Directory.CreateTempSubdirectory("nitro-claude-hook-executor-contention-tests");

        try
        {
            var workspaceRoot = tempRoot.FullName;
            var workspaceDirectory = AgentWorkspace.GetDirectory(workspaceRoot);
            Directory.CreateDirectory(workspaceDirectory);
            var fileSystem = new TestFileSystem(workspaceRoot);
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
            var database = new AgentDatabase();
            var agentRegistry = new AgentRegistry(fileSystem, timeProvider, database);
            var sessions = new AgentSessionRegistry(
                fileSystem,
                timeProvider,
                database,
                agentRegistry,
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(workspaceRoot),
                new ProcessInfoProvider(),
                new FixedAncestorSessionResolver(null));
            var ledger = new SessionDeliveryLedger(fileSystem, database);
            var mail = new MailStore(fileSystem, timeProvider, database, agentRegistry);
            var environmentVariables = new FixedEnvironmentVariableProvider();
            var handler = new ClaudeHookHandler(
                fileSystem,
                timeProvider,
                sessions,
                ledger,
                mail,
                environmentVariables,
                new ProcessInfoProvider(),
                new FixedAncestorSessionResolver(null),
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(workspaceRoot));

            await using (await database.InitializeAsync(workspaceDirectory, cancellationToken))
            {
            }

            environmentVariables.Set("NITRO_MAIL_ACTOR", "alice");
            var payload = new ClaudeHookPayload { SessionId = "session-1", Cwd = workspaceRoot };
            await handler.HandleSessionStartAsync(payload, dryRun: true, cancellationToken);
            await mail.SendMessageAsync(
                new MailMessageCreation { Sender = "bob", Subject = "status", Body = "check", To = ["alice"] },
                cancellationToken);

            await using var lockConnection = new SqliteConnection(
                $"Data Source={AgentWorkspace.GetDatabasePath(workspaceDirectory)};Pooling=False");
            await lockConnection.OpenAsync(cancellationToken);
            await using var lockTransaction = lockConnection.BeginTransaction();
            await using (var lockCommand = lockConnection.CreateCommand())
            {
                lockCommand.Transaction = lockTransaction;
                lockCommand.CommandText = "UPDATE agent_sessions SET last_beat_at = last_beat_at;";
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var input = new StringReader(
                $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(workspaceRoot)}}}""");
            var output = new StringWriter();

            // act
            var exitCode = await ClaudeHookExecutor.RunAsync(
                environmentVariables,
                input,
                output,
                (p, ct) => handler.HandleStopAsync(p, dryRun: true, ct),
                TimeSpan.FromMilliseconds(200),
                cancellationToken);

            // assert
            Assert.Equal(0, exitCode);
            Assert.Equal("{}", output.ToString().Trim());
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_SchemaVersionMismatches()
    {
        // arrange: the workspace database is stamped with a schema version
        // newer than AgentDatabase.CurrentVersion, so the handler's own
        // connection attempt throws ExitException; the executor's fail-open
        // envelope must still resolve to neutral instead of surfacing it.
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = Directory.CreateTempSubdirectory("nitro-claude-hook-executor-version-tests");

        try
        {
            var workspaceRoot = tempRoot.FullName;
            var workspaceDirectory = AgentWorkspace.GetDirectory(workspaceRoot);
            Directory.CreateDirectory(workspaceDirectory);
            var fileSystem = new TestFileSystem(workspaceRoot);
            var timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
            var database = new AgentDatabase();
            var agentRegistry = new AgentRegistry(fileSystem, timeProvider, database);
            var sessions = new AgentSessionRegistry(
                fileSystem,
                timeProvider,
                database,
                agentRegistry,
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(workspaceRoot),
                new ProcessInfoProvider(),
                new FixedAncestorSessionResolver(null));
            var ledger = new SessionDeliveryLedger(fileSystem, database);
            var mail = new MailStore(fileSystem, timeProvider, database, agentRegistry);
            var environmentVariables = new FixedEnvironmentVariableProvider();
            var handler = new ClaudeHookHandler(
                fileSystem,
                timeProvider,
                sessions,
                ledger,
                mail,
                environmentVariables,
                new ProcessInfoProvider(),
                new FixedAncestorSessionResolver(null),
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(workspaceRoot));

            await using (await database.InitializeAsync(workspaceDirectory, cancellationToken))
            {
            }

            await using (var versionConnection = new SqliteConnection(
                $"Data Source={AgentWorkspace.GetDatabasePath(workspaceDirectory)};Pooling=False"))
            {
                await versionConnection.OpenAsync(cancellationToken);
                await using var versionCommand = versionConnection.CreateCommand();
                versionCommand.CommandText = $"PRAGMA user_version = {AgentDatabase.CurrentVersion + 1};";
                await versionCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var input = new StringReader(
                $$"""{"session_id":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(workspaceRoot)}}}""");
            var output = new StringWriter();

            // act
            var exitCode = await ClaudeHookExecutor.RunAsync(
                environmentVariables,
                input,
                output,
                (p, ct) => handler.HandleSessionStartAsync(p, dryRun: true, ct),
                cancellationToken);

            // assert
            Assert.Equal(0, exitCode);
            Assert.Equal("{}", output.ToString().Trim());
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_Should_WriteHookSpecificOutput_When_HandlerReturnsAdditionalContext()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("user-prompt-submit.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => Task.FromResult(new ClaudeHookOutcome { AdditionalContext = "nitro mail: 1 unread message." }),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(
            """{"hookSpecificOutput":{"additionalContext":"nitro mail: 1 unread message."}}""",
            output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteBlockDecision_When_HandlerReturnsBlock()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read("stop.json"));
        var output = new StringWriter();

        // act
        var exitCode = await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (_, _) => Task.FromResult(new ClaudeHookOutcome { Block = true, BlockReason = "unread mail" }),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("""{"decision":"block","reason":"unread mail"}""", output.ToString().Trim());
    }

    [Theory]
    [InlineData("session-start.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    [InlineData("user-prompt-submit.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    [InlineData("stop.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    [InlineData("stop-reentrant.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", true)]
    [InlineData("session-end.json", "5b1c9a3e-4b2f-4a3c-9e3a-2f6b0c1d9e21", "/work/repo", false)]
    public async Task RunAsync_Should_ParseTheCapturedFixture_Into_TheExpectedPayload(
        string fixtureFile, string expectedSessionId, string expectedCwd, bool expectedStopHookActive)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(HookFixtures.Read(fixtureFile));
        var output = new StringWriter();
        ClaudeHookPayload? captured = null;

        // act
        await ClaudeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            (payload, _) =>
            {
                captured = payload;
                return Task.FromResult(ClaudeHookOutcome.Neutral);
            },
            cancellationToken);

        // assert
        Assert.NotNull(captured);
        Assert.Equal(expectedSessionId, captured.SessionId);
        Assert.Equal(expectedCwd, captured.Cwd);
        Assert.Equal(expectedStopHookActive, captured.StopHookActive);
    }
}
