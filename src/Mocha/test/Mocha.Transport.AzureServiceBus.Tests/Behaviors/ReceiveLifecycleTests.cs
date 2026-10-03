using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

/// <summary>
/// Covers receive endpoint disposal idempotency against a live namespace.
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
    public async Task StopAsync_Should_BeIdempotent_When_CalledOnReplyEndpoint()
    {
        // arrange
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

        // act
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);
        await replyEndpoint.StopAsync(runtime, Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.False(replyEndpoint.IsStarted);
    }
}
