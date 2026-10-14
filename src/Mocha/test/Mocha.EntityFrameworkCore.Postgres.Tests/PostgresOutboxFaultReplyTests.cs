using Microsoft.Extensions.DependencyInjection;
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
        await using var host = await StartHostAsync();

        // The caller addresses its reply queue under its own host name. The handler host owns no
        // dispatch endpoint for that address, only its reply dispatch endpoint can deliver there.
        var runtime = (MessagingRuntime)host.Services.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var replyAddress = transport.ReplyReceiveEndpoint!.Source.Address;
        var callerReplyAddress = new UriBuilder(replyAddress) { Host = "caller" }.Uri;

        using var scope = host.Services.CreateScope();
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

    private async Task<MessagingTestHost> StartHostAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();

        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));

        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresOutbox());
        builder.AddRequestHandler<FailingRequestHandler>();
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        return await MessagingTestHost.StartAsync(provider, TestContext.Current.CancellationToken);
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
}
