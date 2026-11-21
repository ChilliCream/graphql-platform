using System.Collections.Concurrent;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Outbox;
using Mocha.Transport.InMemory;
using Npgsql;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxSleepLogTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_NotLogSleep_When_NextMessageIsDueAfterPollingInterval()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var logger = new CapturingLogger();
        await using var provider = await CreateProviderAsync(connectionString, TimeSpan.FromMilliseconds(50), logger);
        await InsertMessageInRetryDelayAsync(connectionString, TimeSpan.FromHours(1));
        var thirdPollEnded = ObservePolls(provider, count: 3);

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await thirdPollEnded.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task ProcessAsync_Should_LogSleep_When_NextMessageIsDueBeforePollingInterval()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var logger = new CapturingLogger();
        await using var provider = await CreateProviderAsync(connectionString, TimeSpan.FromHours(1), logger);
        await InsertMessageInRetryDelayAsync(connectionString, TimeSpan.FromMinutes(1));

        // act
        await using var host = await MessagingTestHost.StartAsync(provider, TestToken);
        var entry = await logger.FirstEntry.WaitAsync(s_timeout, TestToken);

        // assert
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("OutboxProcessorSleeping", entry.EventId.Name);
    }

    private static Task ObservePolls(ServiceProvider provider, int count)
    {
        var pollsEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var polls = 0;
        var options = provider.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
            .Get(typeof(TestDbContext).FullName);
        var createConnection = options.CreateConnection;
        options.CreateConnection = services =>
        {
            var connection = createConnection(services);
            connection.StateChange += (_, args) =>
            {
                if (args.CurrentState == ConnectionState.Closed && Interlocked.Increment(ref polls) == count)
                {
                    pollsEnded.TrySetResult();
                }
            };
            return connection;
        };

        return pollsEnded.Task;
    }

    private static async Task InsertMessageInRetryDelayAsync(string connectionString, TimeSpan dueIn)
    {
        var envelope = new MessageEnvelope { MessageId = Guid.NewGuid().ToString() };
        var id = await MessageRows.InsertOutboxAsync(connectionString, envelope, timesSent: 1, TestToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """UPDATE "outbox_messages" SET "created_at" = NOW() + @dueIn WHERE "id" = @id;""";
        command.Parameters.AddWithValue("@dueIn", dueIn);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(TestToken);
    }

    private static async Task<ServiceProvider> CreateProviderAsync(
        string connectionString,
        TimeSpan pollingInterval,
        CapturingLogger logger)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILogger<PostgresOutboxProcessor>>(logger);
        services.AddKeyedSingleton<IOutboxSignal, StubOutboxSignal>(typeof(TestDbContext));
        services.AddDbContext<TestDbContext>(options => options.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresOutbox(options => options.PollingInterval = pollingInterval));
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message);

    private sealed class CapturingLogger : ILogger<PostgresOutboxProcessor>
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();
        private readonly TaskCompletionSource<LogEntry> _firstEntry =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyCollection<LogEntry> Entries => _entries;

        public Task<LogEntry> FirstEntry => _firstEntry.Task;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var entry = new LogEntry(logLevel, eventId, formatter(state, exception));
            _entries.Enqueue(entry);
            _firstEntry.TrySetResult(entry);
        }
    }
}
