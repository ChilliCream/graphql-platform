using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Events;
using Mocha.Outbox;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresOutboxFaultReplyTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task Outbox_Should_DeliverFaultReply_When_ReplyAddressIsNotADispatchEndpoint()
    {
        // arrange
        await using var env = await CreateBusWithOutboxAsync();

        // The caller addresses its reply queue under its own host name. The handler host owns no
        // dispatch endpoint for that address, only its reply dispatch endpoint can deliver there.
        var transport = env.Runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var replyAddress = transport.ReplyReceiveEndpoint!.Source.Address;
        var callerReplyAddress = new UriBuilder(replyAddress) { Host = "caller" }.Uri;

        using var scope = env.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var request = bus.RequestAsync(
            new FailingRequest(),
            new SendOptions { ReplyEndpoint = callerReplyAddress },
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<RemoteErrorException>(
            async () => await request.AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken));
        Assert.Contains(nameof(InvalidOperationException), exception.Message);
    }

    private async Task<TestEnvironment> CreateBusWithOutboxAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        services.AddSingleton<IOutboxSignal, ResilientOutboxSignal>();

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddRequestHandler<FailingRequestHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(TestContext.Current.CancellationToken);

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var service in hostedServices)
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
        }

        return new TestEnvironment(provider, runtime, hostedServices);
    }

    public sealed class FailingRequest : IEventRequest<FailingResponse>;

    public sealed class FailingResponse;

    public sealed class FailingRequestHandler : IEventRequestHandler<FailingRequest, FailingResponse>
    {
        public ValueTask<FailingResponse> HandleAsync(FailingRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("The request handler failed deliberately.");
        }
    }

    private sealed class TestEnvironment(
        ServiceProvider provider,
        MessagingRuntime runtime,
        List<IHostedService> hostedServices)
        : IAsyncDisposable
    {
        public ServiceProvider Provider => provider;

        public MessagingRuntime Runtime => runtime;

        public async ValueTask DisposeAsync()
        {
            foreach (var service in hostedServices)
            {
                await service.StopAsync(CancellationToken.None);
            }

            // ContinuousTask.DisposeAsync cancels but does not await the background loop, so
            // in-flight processor transactions need a moment to drain before disposal.
            await Task.Delay(250);

            await provider.DisposeAsync();
        }
    }
}
