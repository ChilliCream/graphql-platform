using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="OpencodeHookExecutor"/>'s fail-open envelope
/// directly against <see cref="StringReader"/>/<see cref="StringWriter"/>,
/// mirroring <c>ClaudeHookExecutorTests</c> and
/// <c>CodexHookExecutorTests</c>: the suppression short-circuit, every
/// failure path resolving to the neutral <c>{}</c> response except a
/// schema mismatch (stderr, exit 1), and successful translation of the
/// captured opencode payload fixtures into the wire shape.
/// </summary>
public sealed class OpencodeHookExecutorTests
{
    [Fact]
    public async Task RunAsync_Should_WriteNeutralWithoutInvokingTheHandler_When_SuppressEnvVarIsSet()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var environmentVariables = new FixedEnvironmentVariableProvider();
        environmentVariables.Set("NITRO_HOOK_SUPPRESS", "1");
        var input = new StringReader(OpencodeHookFixtures.Read("session-created.json"));
        var output = new StringWriter();
        var error = new StringWriter();
        var handlerInvoked = false;

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            environmentVariables,
            input,
            output,
            error,
            (_, _) =>
            {
                handlerInvoked = true;
                return Task.FromResult(OpencodeHookOutcome.Neutral);
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
        var input = new StringReader(OpencodeHookFixtures.Read("malformed.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
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
        var input = new StringReader(OpencodeHookFixtures.Read("session-idle.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (_, _) => throw new InvalidOperationException("simulated database contention"),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutralAndExitWithFailure_When_HandlerThrowsSchemaMismatch()
    {
        // arrange: a stale workspace schema keeps every hook of every
        // session inert until someone migrates it, so it must surface on
        // stderr with a nonzero exit rather than disappear into the
        // neutral fail-open response like every other failure.
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(OpencodeHookFixtures.Read("session-idle.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (_, _) => throw new AgentWorkspaceSchemaMismatchException(
                "The agent workspace database has schema v1, expected v2. Run `nitro agent init` to migrate it."),
            cancellationToken);

        // assert
        Assert.Equal(1, exitCode);
        Assert.Equal("{}", output.ToString().Trim());
        Assert.Contains("Run `nitro agent init` to migrate it.", error.ToString());
    }

    [Fact]
    public async Task RunAsync_Should_WriteNeutral_When_TheEntryTimeoutElapses()
    {
        // arrange: an explicit short timeout stands in for the real 10s
        // entry ceiling so this test does not have to wait it out.
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(OpencodeHookFixtures.Read("session-idle.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            async (_, ct) => { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return OpencodeHookOutcome.Neutral; },
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
        var input = new StringReader(OpencodeHookFixtures.Read("session-idle.json"));
        var output = new StringWriter();
        var error = new StringWriter();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            async (_, _) => { await Task.Delay(Timeout.InfiniteTimeSpan); return OpencodeHookOutcome.Neutral; },
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
        // the workspace database, so the session-idle handler's own write
        // (the heartbeat touch) blocks waiting for the lock. The
        // executor's short timeout must still resolve to neutral instead
        // of waiting out SQLite's own (far longer) default busy timeout.
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = Directory.CreateTempSubdirectory("nitro-opencode-hook-executor-contention-tests");

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
                new FixedGlobalConfigDirectoryProvider(workspaceRoot));
            var ledger = new SessionDeliveryLedger(fileSystem, database);
            var mail = new MailStore(fileSystem, timeProvider, database, agentRegistry);
            var environmentVariables = new FixedEnvironmentVariableProvider();
            var handler = new OpencodeHookHandler(
                fileSystem,
                timeProvider,
                sessions,
                ledger,
                mail,
                environmentVariables,
                new FixedInstanceIdProvider("host-1"),
                new FixedGlobalConfigDirectoryProvider(workspaceRoot));

            await using (await database.InitializeAsync(workspaceDirectory, cancellationToken))
            {
            }

            var payload = new OpencodeHookPayload
            {
                SessionId = "session-1",
                Cwd = workspaceRoot,
                ServerUrl = "http://127.0.0.1:4096"
            };
            await handler.HandleSessionCreatedAsync(payload, dryRun: true, cancellationToken);

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
                $$"""
                {"sessionId":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(workspaceRoot)}},"serverUrl":"http://127.0.0.1:4096"}
                """);
            var output = new StringWriter();
            var error = new StringWriter();

            // act
            var exitCode = await OpencodeHookExecutor.RunAsync(
                environmentVariables,
                input,
                output,
                error,
                (p, ct) => handler.HandleSessionIdleAsync(p, dryRun: true, ct),
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
    public async Task RunAsync_Should_WriteNeutral_When_SchemaVersionIsNewerThanSupported()
    {
        // arrange: the workspace database is stamped with a schema version
        // newer than AgentDatabase.CurrentVersion, so the handler's own
        // connection attempt throws the generic ExitException (not the
        // migratable AgentWorkspaceSchemaMismatchException); the executor's
        // fail-open envelope must still resolve to neutral instead of
        // surfacing it.
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = Directory.CreateTempSubdirectory("nitro-opencode-hook-executor-version-tests");

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
                new FixedGlobalConfigDirectoryProvider(workspaceRoot));
            var ledger = new SessionDeliveryLedger(fileSystem, database);
            var mail = new MailStore(fileSystem, timeProvider, database, agentRegistry);
            var handler = new OpencodeHookHandler(
                fileSystem,
                timeProvider,
                sessions,
                ledger,
                mail,
                new FixedEnvironmentVariableProvider(),
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
                $$"""
                {"sessionId":"session-1","cwd":{{System.Text.Json.JsonSerializer.Serialize(workspaceRoot)}},"serverUrl":"http://127.0.0.1:4096"}
                """);
            var output = new StringWriter();
            var error = new StringWriter();

            // act
            var exitCode = await OpencodeHookExecutor.RunAsync(
                new FixedEnvironmentVariableProvider(),
                input,
                output,
                error,
                (p, ct) => handler.HandleSessionCreatedAsync(p, dryRun: true, ct),
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
    public async Task RunAsync_Should_WriteParts_When_HandlerReturnsParts()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(OpencodeHookFixtures.Read("chat-message.json"));
        var output = new StringWriter();
        var error = new StringWriter();

        // act
        var exitCode = await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (_, _) => Task.FromResult(
                new OpencodeHookOutcome { Parts = ["You have 1 unread nitro message."] }),
            cancellationToken);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Equal(
            """{"parts":["You have 1 unread nitro message."]}""",
            output.ToString().Trim());
    }

    [Theory]
    [InlineData("session-created.json", "ses_01a02e51c25775c3b242b56199a18839", "secret", "1.18.25")]
    [InlineData("chat-message.json", "ses_01a02e51c25775c3b242b56199a18839", null, null)]
    [InlineData("session-idle.json", "ses_01a02e51c25775c3b242b56199a18839", null, null)]
    [InlineData("session-deleted.json", "ses_01a02e51c25775c3b242b56199a18839", null, null)]
    public async Task RunAsync_Should_ParseTheFixture_Into_TheExpectedPayload(
        string fixtureFile, string expectedSessionId, string? expectedServerPassword, string? expectedHarnessVersion)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var input = new StringReader(OpencodeHookFixtures.Read(fixtureFile));
        var output = new StringWriter();
        var error = new StringWriter();
        OpencodeHookPayload? captured = null;

        // act
        await OpencodeHookExecutor.RunAsync(
            new FixedEnvironmentVariableProvider(),
            input,
            output,
            error,
            (payload, _) =>
            {
                captured = payload;
                return Task.FromResult(OpencodeHookOutcome.Neutral);
            },
            cancellationToken);

        // assert
        Assert.NotNull(captured);
        Assert.Equal(expectedSessionId, captured.SessionId);
        Assert.Equal("http://127.0.0.1:4096", captured.ServerUrl);
        Assert.Equal(expectedServerPassword, captured.ServerPassword);
        Assert.Equal(expectedHarnessVersion, captured.HarnessVersion);
    }
}
