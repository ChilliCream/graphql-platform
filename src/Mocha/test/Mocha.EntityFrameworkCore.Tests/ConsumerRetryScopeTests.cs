using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Tests;

/// <summary>
/// Tests that an in-process retry does not re-run a handler against the <see cref="DbContext"/>
/// the failed attempt left behind, which would replay its pending changes on top of the retry's.
/// </summary>
public sealed class ConsumerRetryScopeTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Retry_Should_InsertOnce_When_HandlerFailsAfterAddAndSucceedsOnRetry()
    {
        // arrange
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var saved = new TaskCompletionSource();
        var attempts = new AttemptCounter();

        var services = new ServiceCollection();
        services.AddDbContext<RetryDbContext>(o => o.UseSqlite(connection));
        services.AddSingleton(saved);
        services.AddSingleton(attempts);
        var builder = services.AddMessageBus()
            .AddResilience(p => p.On<Exception>().Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant))
            .AddEventHandler<AddThenThrowOnceHandler>();
        builder.AddInMemory();

        await using var provider = services.BuildServiceProvider();
        using (var setup = provider.CreateScope())
        {
            await setup.ServiceProvider.GetRequiredService<RetryDbContext>().Database
                .EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new CreateRow("only-once"), CancellationToken.None);
        await saved.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert - the retry saved exactly one row, not the failed attempt's row as well
        using var verify = provider.CreateScope();
        var rows = await verify.ServiceProvider.GetRequiredService<RetryDbContext>().Rows
            .Select(r => r.Name)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, attempts.Value);
        Assert.Equal(["only-once"], rows);
    }

    public sealed class AttemptCounter
    {
        public int Value;
    }

    public sealed record CreateRow(string Name);

    public sealed class Row
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    public sealed class RetryDbContext(DbContextOptions<RetryDbContext> options) : DbContext(options)
    {
        public DbSet<Row> Rows => Set<Row>();
    }

    /// <summary>
    /// Adds its row, throws on the first attempt before saving, and saves on the retry.
    /// </summary>
    public sealed class AddThenThrowOnceHandler(RetryDbContext db, AttemptCounter attempts, TaskCompletionSource saved)
        : IEventHandler<CreateRow>
    {
        public async ValueTask HandleAsync(CreateRow message, CancellationToken cancellationToken)
        {
            db.Rows.Add(new Row { Name = message.Name });

            if (Interlocked.Increment(ref attempts.Value) == 1)
            {
                throw new InvalidOperationException("transient");
            }

            await db.SaveChangesAsync(cancellationToken);
            saved.TrySetResult();
        }
    }
}
