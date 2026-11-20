using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Mocha.EntityFrameworkCore;
using Mocha.Scheduling;

namespace Mocha.Tests.Scheduling;

public sealed class SchedulingInterceptorTests : IDisposable
{
    private static readonly DateTimeOffset s_baseTime =
        new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly SchedulingDbTransactionInterceptor s_transactionInterceptor = new();
    private static readonly SchedulingSaveChangesInterceptor s_saveChangesInterceptor = new();

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
        var signal = new RecordingSchedulerSignal();
        using var services = CreateServices(signal);
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        using var transaction = dbContext.Database.BeginTransaction();

        // act
        transaction.Commit();

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    [Fact]
    public async Task TransactionCommittedAsync_Should_NotifySignalWithExactTime_When_Called()
    {
        // arrange
        var signal = new RecordingSchedulerSignal();
        await using var services = CreateServices(signal);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);

        // act
        await transaction.CommitAsync(TestContext.Current.CancellationToken);

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    [Fact]
    public void SavedChanges_Should_NotifySignal_When_NoAmbientTransaction()
    {
        // arrange
        var signal = new RecordingSchedulerSignal();
        using var services = CreateServices(signal);
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // act
        dbContext.SaveChanges();

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    [Fact]
    public async Task SavedChanges_Should_NotNotifySignal_When_AmbientTransactionExists()
    {
        // arrange
        var signal = new RecordingSchedulerSignal();
        await using var services = CreateServices(signal);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            TestContext.Current.CancellationToken);

        // act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(signal.Notifications);
    }

    [Fact]
    public async Task SavedChangesAsync_Should_NotifySignal_When_NoAmbientTransaction()
    {
        // arrange
        var signal = new RecordingSchedulerSignal();
        await using var services = CreateServices(signal);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // act
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // assert
        var notification = Assert.Single(signal.Notifications);
        Assert.Equal(s_baseTime, notification);
    }

    private ServiceProvider CreateServices(RecordingSchedulerSignal signal)
    {
        var messagingServices = new MessagingDbContextServices(
            outboxSignal: null,
            signal,
            new FakeTimeProvider(s_baseTime));
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options
            .UseSqlite(_connection)
            .AddMessagingExtensions(new MessagingDbContextOptions(), messagingServices)
            .AddInterceptors(s_transactionInterceptor, s_saveChangesInterceptor));

        return services.BuildServiceProvider();
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

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
