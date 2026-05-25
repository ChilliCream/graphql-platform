using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Mocha.EntityFrameworkCore;
using Mocha.Scheduling;

#pragma warning disable EF1001 // LoggingOptions is an internal EF Core API, used here only to construct event data for tests.

namespace Mocha.Tests.Scheduling;

public sealed class SchedulingInterceptorTests : IDisposable
{
    private static readonly DateTimeOffset s_baseTime =
        new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection;

    public SchedulingInterceptorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public void TransactionCommitted_Should_NotifySignalWithExactTime_When_Called()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        var signal = new RecordingSchedulerSignal();
        var interceptor = new SchedulingDbTransactionInterceptor(signal, timeProvider);

        // act
        interceptor.TransactionCommitted(null!, null!);

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    [Fact]
    public async Task TransactionCommittedAsync_Should_NotifySignalWithExactTime_When_Called()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        var signal = new RecordingSchedulerSignal();
        var interceptor = new SchedulingDbTransactionInterceptor(signal, timeProvider);

        // act
        await interceptor.TransactionCommittedAsync(null!, null!);

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    [Fact]
    public async Task SavedChanges_Should_NotifySignal_When_NoAmbientTransaction()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        var signal = new RecordingSchedulerSignal();
        var interceptor = new SchedulingSaveChangesInterceptor(signal, timeProvider);

        await using var dbContext = CreateDbContext();
        var eventData = CreateSaveChangesEventData(dbContext);

        // act  no transaction is open on the context
        interceptor.SavedChanges(eventData, 1);

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    [Fact]
    public async Task SavedChanges_Should_NotNotifySignal_When_AmbientTransactionExists()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        var signal = new RecordingSchedulerSignal();
        var interceptor = new SchedulingSaveChangesInterceptor(signal, timeProvider);

        await using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();

        // Begin a transaction so CurrentTransaction is non-null
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var eventData = CreateSaveChangesEventData(dbContext);

        // act
        interceptor.SavedChanges(eventData, 1);

        // assert  signal should NOT have been notified
        Assert.Empty(signal.Notifications);
    }

    [Fact]
    public void SavedChanges_Should_ReturnResult_When_ContextIsNull()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        var signal = new RecordingSchedulerSignal();
        var interceptor = new SchedulingSaveChangesInterceptor(signal, timeProvider);

        var eventData = CreateSaveChangesEventData(context: null);

        // act
        var result = interceptor.SavedChanges(eventData, 42);

        // assert  result passes through, no notification
        Assert.Equal(42, result);
        Assert.Empty(signal.Notifications);
    }

    [Fact]
    public async Task SavedChangesAsync_Should_NotifySignal_When_NoAmbientTransaction()
    {
        // arrange
        var timeProvider = new FakeTimeProvider(s_baseTime);
        var signal = new RecordingSchedulerSignal();
        var interceptor = new SchedulingSaveChangesInterceptor(signal, timeProvider);

        await using var dbContext = CreateDbContext();
        var eventData = CreateSaveChangesEventData(dbContext);

        // act
        var result = await interceptor.SavedChangesAsync(eventData, 1);

        // assert
        Assert.Equal(1, result);
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    private TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new TestDbContext(options);
    }

    private static SaveChangesCompletedEventData CreateSaveChangesEventData(DbContext? context)
    {
        var loggingOptions = new LoggingOptions();
        var eventDefinition = new StubEventDefinition(
            loggingOptions,
            new EventId(1, "TestSaveChanges"),
            LogLevel.Information,
            "TestSaveChanges");

        return new SaveChangesCompletedEventData(
            eventDefinition,
            static (_, _) => "test",
            context!,
            entitiesSavedCount: 0);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private sealed class RecordingSchedulerSignal : ISchedulerSignal
    {
        public List<DateTimeOffset> Notifications { get; } = [];

        public void Notify(DateTimeOffset scheduledTime)
        {
            Notifications.Add(scheduledTime);
        }

        public Task WaitUntilAsync(DateTimeOffset wakeTime, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubEventDefinition(
        ILoggingOptions loggingOptions,
        EventId eventId,
        LogLevel level,
        string eventIdCode)
        : EventDefinitionBase(loggingOptions, eventId, level, eventIdCode);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
