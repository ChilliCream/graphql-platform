using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Events;
using Mocha.Middlewares;
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
        await env.StartWorkerAsync();

        // The caller addresses its reply queue under its own host name. The handler host owns no
        // dispatch endpoint for that address, only its reply dispatch endpoint can deliver there.
        var callerReplyAddress = env.GetCallerReplyAddress();

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

    [Fact]
    public async Task Outbox_Should_DropMessage_When_DestinationCannotBeResolved()
    {
        // arrange
        var logs = new CapturingLoggerProvider();
        await using var env = await CreateBusWithOutboxAsync(logs);

        var envelope = env.CreateFaultEnvelope(
            env.Runtime.GetMessageType(typeof(FailingResponse)).Identity,
            "unknown://caller/q/replies");

        var id = await env.PersistAsync(envelope);

        // act
        await env.StartWorkerAsync();

        // assert
        var message = await logs.WaitForAsync(LogLevel.Critical, s_timeout);
        Assert.Equal(
            "Could not resolve a dispatch endpoint for the destination unknown://caller/q/replies "
                + $"of message with ID {id}. Message Dropped.",
            message);
        Assert.Equal(0, await env.CountOutboxMessagesAsync());
    }

    [Fact]
    public async Task Outbox_Should_DropMessage_When_MessageTypeIsUnknown()
    {
        // arrange
        var logs = new CapturingLoggerProvider();
        await using var env = await CreateBusWithOutboxAsync(logs);

        var envelope = env.CreateFaultEnvelope("Unknown.Message", env.GetCallerReplyAddress().ToString());

        var id = await env.PersistAsync(envelope);

        // act
        await env.StartWorkerAsync();

        // assert
        var message = await logs.WaitForAsync(LogLevel.Critical, s_timeout);
        Assert.Equal(
            $"Could not resolve the message type Unknown.Message of message with ID {id}. Message Dropped.",
            message);
        Assert.Equal(0, await env.CountOutboxMessagesAsync());
    }

    private async Task<TestEnvironment> CreateBusWithOutboxAsync(CapturingLoggerProvider? logs = null)
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            if (logs is not null)
            {
                logging.AddProvider(logs);
            }
        });
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

        return new TestEnvironment(provider, runtime);
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

    private sealed class TestEnvironment(ServiceProvider provider, MessagingRuntime runtime) : IAsyncDisposable
    {
        private readonly List<IHostedService> _hostedServices = provider.GetServices<IHostedService>().ToList();
        private bool _started;

        public ServiceProvider Provider => provider;

        public MessagingRuntime Runtime => runtime;

        public async Task StartWorkerAsync()
        {
            foreach (var service in _hostedServices)
            {
                await service.StartAsync(TestContext.Current.CancellationToken);
            }

            _started = true;
        }

        /// <summary>
        /// Returns the host's own reply queue addressed under a foreign host name, the shape of a
        /// reply address that belongs to a caller from another service.
        /// </summary>
        public Uri GetCallerReplyAddress()
        {
            var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
            var replyAddress = transport.ReplyReceiveEndpoint!.Source.Address;
            return new UriBuilder(replyAddress) { Host = "caller" }.Uri;
        }

        public MessageEnvelope CreateFaultEnvelope(string messageType, string destinationAddress)
        {
            var headers = new Headers();
            headers.Set(MessageHeaders.MessageKind, MessageKind.Fault);

            return new MessageEnvelope
            {
                MessageId = Guid.NewGuid().ToString(),
                MessageType = messageType,
                DestinationAddress = destinationAddress,
                ContentType = "application/json",
                Headers = headers
            };
        }

        public async Task<Guid> PersistAsync(MessageEnvelope envelope)
        {
            using var scope = provider.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IMessageOutbox>();
            await outbox.PersistAsync(envelope, TestContext.Current.CancellationToken);

            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            return await db.Set<OutboxMessage>()
                .Select(m => m.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        public async Task<int> CountOutboxMessagesAsync()
        {
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            return await db.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (_started)
            {
                foreach (var service in _hostedServices)
                {
                    await service.StopAsync(CancellationToken.None);
                }

                // ContinuousTask.DisposeAsync cancels but does not await the background loop, so
                // in-flight processor transactions need a moment to drain before disposal.
                await Task.Delay(250);
            }

            await provider.DisposeAsync();
        }
    }
}
