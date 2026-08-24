using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

/// <summary>
/// Covers the receive endpoint start/stop lifecycle against a live namespace: disposal
/// idempotency and restart after stop.
/// </summary>
[Collection("AzureServiceBus")]
public class ReceiveLifecycleTests
{
    private readonly AzureServiceBusFixture _fixture;

    public ReceiveLifecycleTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StopAsync_Should_BeIdempotentAndAllowRestart_When_CalledOnReplyEndpoint()
    {
        // arrange - the reply receive endpoint runs both a processor and a queue heartbeat, so
        // stopping it exercises the disposal of both resources together.
        await using var ctx = _fixture.CreateTestContext();
        await using var bus = await new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddAzureServiceBus(ctx)
            .BuildTestBusAsync();

        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var replyEndpoint = transport.ReplyReceiveEndpoint
            ?? throw new InvalidOperationException("Expected a reply receive endpoint to be configured.");
        Assert.True(replyEndpoint.IsStarted);

        // act - stop twice in a row; the second call must be a safe no-op
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.False(replyEndpoint.IsStarted);

        // act - restarting proves the processor and heartbeat from the stopped endpoint were
        // fully released rather than left bound to disposed links
        await replyEndpoint.StartAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.True(replyEndpoint.IsStarted);
    }
}
