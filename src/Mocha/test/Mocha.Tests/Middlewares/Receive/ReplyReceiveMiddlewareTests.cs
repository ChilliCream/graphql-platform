using Mocha.Features;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for the ReplyReceiveMiddleware which adds a ReplyConsumer to the receive pipeline,
/// enabling request-response message patterns via the DeferredResponseManager.
/// </summary>
public class ReplyReceiveMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_AddConsumerToFeature_When_Invoked()
    {
        // arrange
        var consumer = CreateReplyConsumer();
        var middleware = new ReplyReceiveMiddleware(consumer);
        var context = new StubReceiveContext();
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.Contains(consumer, feature.Consumers);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_Invoked()
    {
        // arrange
        var consumer = CreateReplyConsumer();
        var middleware = new ReplyReceiveMiddleware(consumer);
        var context = new StubReceiveContext();
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(tracker.WasInvoked, "Next delegate should be called");
    }

    [Fact]
    public async Task InvokeAsync_Should_AddConsumerBeforeCallingNext()
    {
        // arrange
        var consumer = CreateReplyConsumer();
        var middleware = new ReplyReceiveMiddleware(consumer);
        var context = new StubReceiveContext();
        var consumerWasAdded = false;

        ReceiveDelegate next = ctx =>
        {
            var feature = ctx.Features.GetOrSet<ReceiveConsumerFeature>();
            consumerWasAdded = feature.Consumers.Contains(consumer);
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert - consumer should already be in the set when next is called
        Assert.True(consumerWasAdded, "Consumer should be added to the feature before calling next");
    }

    [Fact]
    public async Task InvokeAsync_Should_AddSameConsumerOnMultipleInvocations()
    {
        // arrange - verify the same consumer instance is reused across invocations
        var consumer = CreateReplyConsumer();
        var middleware = new ReplyReceiveMiddleware(consumer);

        var context1 = new StubReceiveContext();
        var context2 = new StubReceiveContext();
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context1, next);
        await middleware.InvokeAsync(context2, next);

        // assert
        var feature1 = context1.Features.GetOrSet<ReceiveConsumerFeature>();
        var feature2 = context2.Features.GetOrSet<ReceiveConsumerFeature>();
        var consumer1 = Assert.Single(feature1.Consumers);
        var consumer2 = Assert.Single(feature2.Consumers);
        Assert.Same(consumer1, consumer2);
    }

    private static ReplyConsumer CreateReplyConsumer()
    {
        var responseManager = new DeferredResponseManager(TimeProvider.System);
        return new ReplyConsumer(responseManager);
    }
}
