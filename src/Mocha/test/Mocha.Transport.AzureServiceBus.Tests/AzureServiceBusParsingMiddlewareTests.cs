using Azure.Messaging.ServiceBus;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Features;
using Mocha.Transport.AzureServiceBus.Middlewares;

namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class AzureServiceBusParsingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_SetEnvelopeBeforeInvokingNext_When_MessageIsParsed()
    {
        // arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "message-1",
            correlationId: "correlation-1");
        var receiver = new StubServiceBusReceiver();
        var feature = new AzureServiceBusReceiveFeature();
        feature.SetNonSession(new ProcessMessageEventArgs(message, receiver, CancellationToken.None));

        var context = new ReceiveContext();
        context.Features.Set(feature);

        MessageEnvelope? observedEnvelope = null;
        ReceiveDelegate next = ctx =>
        {
            observedEnvelope = ctx.Envelope;
            return default;
        };

        var middleware = new AzureServiceBusParsingMiddleware();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.NotNull(observedEnvelope);
        Assert.Equal("message-1", observedEnvelope!.MessageId);
        Assert.Equal("correlation-1", observedEnvelope.CorrelationId);
    }

    private sealed class StubServiceBusReceiver : ServiceBusReceiver;
}
