using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Inbox;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class InboxServiceRegistrationTests
{
    private const string ConnectionString = "Host=localhost;Database=test";

    [Fact]
    public async Task UsePostgresInbox_Should_RegisterHostedService_When_Called()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var hostedServices = provider.GetServices<IHostedService>();

        // Assert
        Assert.Contains(hostedServices, s => s is MessageBusInboxWorker);
    }

    [Fact]
    public async Task UsePostgresInbox_Should_RegisterScopedInbox_When_Called()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        using var scope = provider.CreateScope();
        var inbox = scope.ServiceProvider.GetService<IMessageInbox>();

        // Assert
        Assert.NotNull(inbox);
        Assert.IsType<PostgresMessageInbox>(inbox);
    }

    [Fact]
    public async Task UsePostgresInbox_Should_ConfigureQueriesFromModel_When_DefaultTableNames()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PostgresMessageInboxOptions>>();
        var contextName = typeof(TestDbContext).FullName!;
        var options = optionsMonitor.Get(contextName);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.Exists));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.Insert));
        Assert.False(string.IsNullOrWhiteSpace(options.Queries.Cleanup));
        Assert.False(string.IsNullOrWhiteSpace(options.ConnectionString));
    }

    [Fact]
    public async Task UsePostgresInbox_Should_UseDefaultInboxOptions_When_NoConfigure()
    {
        // Arrange
        await using var provider = BuildProvider();

        // Act
        var inboxOptions = provider.GetRequiredService<IOptions<InboxOptions>>().Value;

        // Assert
        Assert.Equal(TimeSpan.FromDays(7), inboxOptions.RetentionPeriod);
        Assert.Equal(TimeSpan.FromHours(1), inboxOptions.CleanupInterval);
    }

    [Fact]
    public async Task UsePostgresInbox_Should_UseCustomInboxOptions_When_ConfigureProvided()
    {
        // Arrange
        await using var provider = BuildProvider(configure: opts =>
        {
            opts.RetentionPeriod = TimeSpan.FromDays(14);
            opts.CleanupInterval = TimeSpan.FromMinutes(30);
        });

        // Act
        var inboxOptions = provider.GetRequiredService<IOptions<InboxOptions>>().Value;

        // Assert
        Assert.Equal(TimeSpan.FromDays(14), inboxOptions.RetentionPeriod);
        Assert.Equal(TimeSpan.FromMinutes(30), inboxOptions.CleanupInterval);
    }

    private static ServiceProvider BuildProvider(Action<InboxOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseNpgsql(ConnectionString));

        // Use a resilient signal to prevent ObjectDisposedException when
        // EF Core shares the internal service provider (and interceptors)
        // across test classes via ShouldUseSameServiceProvider.
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(
            ef => ef.UsePostgresInbox(configure));
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        // Build the runtime so that all singleton factories resolve
        _ = provider.GetRequiredService<IMessagingRuntime>();

        return provider;
    }
}
