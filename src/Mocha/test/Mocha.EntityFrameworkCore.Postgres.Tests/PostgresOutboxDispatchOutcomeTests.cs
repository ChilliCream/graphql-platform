using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxDispatchOutcomeTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_ReleaseClaimAndWarn_When_TransportIsUnavailable()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var retrying = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            if (attempt.Number == 1)
            {
                throw new TransportUnavailableException("The broker is unreachable.", null);
            }

            retrying.TrySetResult();
            await resume.Task.WaitAsync(attempt.CancellationToken);
            await attempt.SendAsync();
        });
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, static _ => { });
        var envelope = CreateEnvelope(provider, "A");
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);
        int? timesSentWhileRetrying;
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            await retrying.Task.WaitAsync(s_timeout, TestToken);
            timesSentWhileRetrying = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);
            resume.TrySetResult();
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, timesSentWhileRetrying);
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Equal(
            [
                new LogEntry(
                    LogLevel.Warning,
                    $"The dispatch of {Describe(id, envelope)} failed because the transport is unavailable. "
                    + "The attempt is not counted and the message is dispatched again.")
            ],
            logs.Entries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProcessAsync_Should_ReleaseClaimAndWarn_When_DispatchExceedsDispatchTimeout(
        bool afterCompletedDispatch)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatchTimeout = TimeSpan.FromSeconds(1);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            if (attempt is { MessageId: "A", Number: 1 })
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, attempt.CancellationToken);
            }

            await attempt.SendAsync();
        });
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        // One attempt is all a message has, so a counted timeout would leave the message in the outbox.
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, o =>
        {
            o.DispatchTimeout = dispatchTimeout;
            o.MaxAttempts = 1;
        });
        string[] expectedAccepted = ["A"];
        if (afterCompletedDispatch)
        {
            await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "earlier"), TestToken);
            expectedAccepted = ["A", "earlier"];
        }

        var envelope = CreateEnvelope(provider, "A");
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(expectedAccepted, dispatch.Accepted.Order());
        Assert.Equal(
            [
                new LogEntry(
                    LogLevel.Warning,
                    $"The dispatch of {Describe(id, envelope)} did not complete within the dispatch timeout of "
                    + "00:00:01. The attempt is not counted and the message is dispatched again.")
            ],
            logs.Entries);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ProcessAsync_Should_ReleaseClaimWithoutWarning_When_HostStopsDuringDispatch(
        bool afterCompletedDispatch,
        bool withoutDispatchTimeout)
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            if (attempt.MessageId == "A")
            {
                dispatching.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, attempt.CancellationToken);
            }

            await attempt.SendAsync();
        });
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>(LogLevel.Debug);
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, o =>
        {
            if (withoutDispatchTimeout)
            {
                o.DispatchTimeout = Timeout.InfiniteTimeSpan;
            }
        });
        if (afterCompletedDispatch)
        {
            await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "earlier"), TestToken);
        }

        var envelope = CreateEnvelope(provider, "A");
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using (StartProcessor(provider))
        {
            await dispatching.Task.WaitAsync(s_timeout, TestToken);
        }

        var timesSent = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(0, timesSent);
        Assert.Equal(
            [
                new LogEntry(
                    LogLevel.Debug,
                    $"The dispatch of {Describe(id, envelope)} was cancelled because the host is stopping. "
                    + "The attempt is not counted.")
            ],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_CountAttemptAndLogError_When_DispatchIsRefused()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            if (attempt.Number == 1)
            {
                throw new InvalidOperationException("The broker refused the message.");
            }

            // No later attempt completes, so the outcome of the first one stays in the outbox.
            await Task.Delay(Timeout.InfiniteTimeSpan, attempt.CancellationToken);
        });
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, o => o.MaxAttempts = 3);
        var envelope = CreateEnvelope(provider, "A");
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);
        int? timesSent;

        // act
        await using (StartProcessor(provider))
        {
            timesSent = await MessageRows.WaitForOutboxTimesSentAsync(connectionString, id, 1, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(1, timesSent);
        Assert.Equal(
            [new LogEntry(LogLevel.Error, $"The dispatch of {Describe(id, envelope)} failed on attempt 1 of 3.")],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_LogCritical_When_RefusedDispatchWasTheLastAttempt()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(
            static _ => throw new InvalidOperationException("The broker refused the message."));
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, o => o.MaxAttempts = 1);
        var envelope = CreateEnvelope(provider, "A");
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using (StartProcessor(provider))
        {
            await logs.WaitForEntriesAsync(2, s_timeout, TestToken);
        }

        var timesSent = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(1, timesSent);
        Assert.Equal(
            [
                new LogEntry(LogLevel.Error, $"The dispatch of {Describe(id, envelope)} failed on attempt 1 of 1."),
                new LogEntry(
                    LogLevel.Critical,
                    $"Message with ID {id} exhausted all 1 attempts and will not be retried.")
            ],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_LogCritical_When_UnreadableMessageExhaustsItsAttempts()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(
            connectionString, new ScriptedDispatch(static attempt => attempt.SendAsync()), logs, o => o.MaxAttempts = 1);
        var id = await MessageRows.InsertOutboxAsync(connectionString, "[]", TestToken);

        // act
        await using (StartProcessor(provider))
        {
            await logs.WaitForEntriesAsync(3, s_timeout, TestToken);
        }

        var timesSent = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(1, timesSent);
        Assert.Equal(
            [
                new LogEntry(LogLevel.Error, "Error reading message envelope"),
                new LogEntry(
                    LogLevel.Warning,
                    $"Could not deserialize message body for message with ID {id}. The message will be retried."),
                new LogEntry(
                    LogLevel.Critical,
                    $"Message with ID {id} exhausted all 1 attempts and will not be retried.")
            ],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_LogCritical_When_MessageOfUnknownTypeExhaustsItsAttempts()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(
            connectionString, new ScriptedDispatch(static attempt => attempt.SendAsync()), logs, o => o.MaxAttempts = 1);
        var envelope = new MessageEnvelope(CreateEnvelope(provider, "A"))
        {
            MessageType = "urn:message:Mocha.Tests:UnknownEvent"
        };
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using (StartProcessor(provider))
        {
            await logs.WaitForEntriesAsync(2, s_timeout, TestToken);
        }

        var timesSent = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(1, timesSent);
        Assert.Equal(
            [
                new LogEntry(
                    LogLevel.Warning,
                    "Could not resolve the message type urn:message:Mocha.Tests:UnknownEvent of message with ID "
                    + $"{id}. The message will be retried."),
                new LogEntry(
                    LogLevel.Critical,
                    $"Message with ID {id} exhausted all 1 attempts and will not be retried.")
            ],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_LogCritical_When_MessageWithoutDestinationExhaustsItsAttempts()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(
            connectionString, new ScriptedDispatch(static attempt => attempt.SendAsync()), logs, o => o.MaxAttempts = 1);
        var envelope = new MessageEnvelope(CreateEnvelope(provider, "A"))
        {
            DestinationAddress = "nowhere://unknown"
        };
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using (StartProcessor(provider))
        {
            await logs.WaitForEntriesAsync(2, s_timeout, TestToken);
        }

        var timesSent = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(1, timesSent);
        Assert.Equal(
            [
                new LogEntry(
                    LogLevel.Warning,
                    "Could not resolve a dispatch endpoint for the destination nowhere://unknown of message with ID "
                    + $"{id}. The message will be retried."),
                new LogEntry(
                    LogLevel.Critical,
                    $"Message with ID {id} exhausted all 1 attempts and will not be retried.")
            ],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_StopDispatchingMessage_When_MaxAttemptsIsReached()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(attempt => attempt.MessageId == "A"
            ? throw new InvalidOperationException("The broker refused the message.")
            : attempt.SendAsync());
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, o =>
        {
            o.MaxAttempts = 3;
            o.MaxRetryDelay = TimeSpan.FromMilliseconds(50);
        });
        var envelope = CreateEnvelope(provider, "A");
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, TestToken);

        // act
        await using (StartProcessor(provider))
        {
            await logs.WaitForEntriesAsync(4, s_timeout, TestToken);

            // The exhausted message is due and older than the next message, so a claim would pick it first.
            await MessageRows.MakeOutboxDueAsync(connectionString, TimeSpan.FromHours(1), TestToken);
            await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "B"), TestToken);
            provider.GetRequiredService<IOutboxSignal>().Set();
            await MessageRows.WaitForOutboxCountAsync(connectionString, 1, s_timeout, TestToken);
        }

        var timesSent = await MessageRows.GetOutboxTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(3, timesSent);
        Assert.Equal(3, dispatch.AttemptsOf("A"));
        Assert.Equal(["B"], dispatch.Accepted);
        Assert.Equal(
            [
                new LogEntry(LogLevel.Error, $"The dispatch of {Describe(id, envelope)} failed on attempt 1 of 3."),
                new LogEntry(LogLevel.Error, $"The dispatch of {Describe(id, envelope)} failed on attempt 2 of 3."),
                new LogEntry(LogLevel.Error, $"The dispatch of {Describe(id, envelope)} failed on attempt 3 of 3."),
                new LogEntry(
                    LogLevel.Critical,
                    $"Message with ID {id} exhausted all 3 attempts and will not be retried.")
            ],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_RetryWithinMaxRetryDelay_When_AttemptsKeepFailing()
    {
        // arrange
        // Without the cap the sixth attempt is due more than a minute after the first one.
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(
            static _ => throw new InvalidOperationException("The broker refused the message."));
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, o =>
        {
            o.MaxAttempts = 6;
            o.MaxRetryDelay = TimeSpan.FromMilliseconds(100);
        });
        var id = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        int? timesSent;

        // act
        await using (StartProcessor(provider))
        {
            timesSent = await MessageRows.WaitForOutboxTimesSentAsync(connectionString, id, 6, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(6, timesSent);
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchMessage_When_AttemptCountIsBeyondTheBackoffRange()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.SendAsync());
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(
            connectionString, dispatch, logs, o => o.MaxAttempts = int.MaxValue);
        var id = await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await MessageRows.SetOutboxTimesSentAsync(connectionString, id, 5000, TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Empty(logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_LimitPollingInterval_When_ClaimedMessageHasBeenDueForMonths()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            await release.Task.WaitAsync(attempt.CancellationToken);
            await attempt.SendAsync();
        });
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>(LogLevel.Debug);
        await using var provider = await CreateProviderAsync(
            connectionString, dispatch, logs, static o => o.MaxConcurrentMessages = 2);
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        await MessageRows.MakeOutboxDueAsync(connectionString, TimeSpan.FromDays(60), TestToken);
        long remaining;

        // act
        await using (StartProcessor(provider))
        {
            // The worker without a message computes its polling interval while the other worker holds the claim.
            await logs.WaitForEntriesAsync(1, s_timeout, TestToken);
            release.TrySetResult();
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(
            [new LogEntry(LogLevel.Debug, "Outbox processor is sleeping for 24.00:00:00.")],
            logs.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_KeepRunning_When_OutboxTableCannotBeRead()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatch = new ScriptedDispatch(static attempt => attempt.SendAsync());
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, static _ => { });
        await ExecuteAsync(connectionString, "ALTER TABLE outbox_messages RENAME TO outbox_messages_moved");
        bool runningAfterFailure;
        long remaining;

        // act
        await using (var processor = StartProcessor(provider))
        {
            await logs.WaitForEntriesAsync(1, s_timeout, TestToken);
            runningAfterFailure = processor.IsRunning;
            await ExecuteAsync(connectionString, "ALTER TABLE outbox_messages_moved RENAME TO outbox_messages");
            await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
            remaining = await MessageRows.WaitForOutboxCountAsync(connectionString, 0, s_timeout, TestToken);
        }

        // assert
        Assert.True(runningAfterFailure);
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Equal(
            [new LogEntry(LogLevel.Error, "An unexpected error occurred while processing outbox event")],
            logs.Entries.Distinct());
    }

    [Fact]
    public async Task StopAsync_Should_DeleteMessage_When_TransportAcceptedItBeforeTheStop()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            await attempt.SendAsync();
            accepted.TrySetResult();
            await attempt.WaitForCancellationAsync(Task.Delay(s_timeout));
        });
        var logs = RecordingLoggerProvider.For<PostgresOutboxProcessor>();
        await using var provider = await CreateProviderAsync(connectionString, dispatch, logs, static _ => { });
        await MessageRows.InsertOutboxAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await accepted.Task.WaitAsync(s_timeout, TestToken);

        // act
        await host.DisposeAsync();
        var remaining = await MessageRows.CountOutboxAsync(connectionString, TestToken);

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.Empty(logs.Entries);
    }

    private static string Describe(Guid id, MessageEnvelope envelope)
        => $"outbox message {id} (message ID {envelope.MessageId}, type {envelope.MessageType}) "
            + $"to {envelope.DestinationAddress}";

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider, string messageId)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(OutcomeEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new OutcomeEvent(messageId))
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        ScriptedDispatch dispatch,
        RecordingLoggerProvider logs,
        Action<PostgresOutboxOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace).AddProvider(logs));
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox(configure));
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("outcome");
            t.DispatchEndpoint("outcome").ToQueue("outcome").Publish<OutcomeEvent>();
        });
        builder.ConfigureMessageBus(b => b.UseDispatch(dispatch.Middleware, after: "Serialization"));

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestToken);
    }

    private static RunningProcessor StartProcessor(IServiceProvider provider)
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processing = provider.GetRequiredService<PostgresOutboxProcessor>().ProcessAsync(cancellation.Token);
        return new RunningProcessor(processing, cancellation);
    }

    public sealed record OutcomeEvent(string Name);

    // Stops the processor like a host does and waits until it has returned.
    private sealed class RunningProcessor(Task processing, CancellationTokenSource cancellation) : IAsyncDisposable
    {
        public bool IsRunning => !processing.IsCompleted;

        public async ValueTask DisposeAsync()
        {
            await cancellation.CancelAsync();
            await processing.WaitAsync(s_timeout, TestToken);
            cancellation.Dispose();
        }
    }
}
