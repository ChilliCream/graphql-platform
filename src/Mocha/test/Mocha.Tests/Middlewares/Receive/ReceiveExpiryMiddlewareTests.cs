using Microsoft.Extensions.Time.Testing;
using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for <see cref="ReceiveExpiryMiddleware"/>.
/// Verifies that the middleware correctly drops expired messages and passes through valid ones.
/// </summary>
public class ReceiveExpiryMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_DeliverByIsNull()
    {
        // arrange
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var middleware = new ReceiveExpiryMiddleware(fakeTime);
        var context = new StubReceiveContext { DeliverBy = null };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(tracker.WasInvoked, "Next should be called when DeliverBy is null");
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_DeliverByIsInFuture()
    {
        // arrange
        var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(baseTime);
        var middleware = new ReceiveExpiryMiddleware(fakeTime);
        var context = new StubReceiveContext { DeliverBy = baseTime.AddHours(1) };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.True(tracker.WasInvoked, "Next should be called when DeliverBy is in the future");
    }

    [Fact]
    public async Task InvokeAsync_Should_MarkConsumedAndNotCallNext_When_DeliverByIsInPast()
    {
        // arrange
        var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(baseTime);
        var middleware = new ReceiveExpiryMiddleware(fakeTime);
        var context = new StubReceiveContext { DeliverBy = baseTime.AddHours(-1) };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert
        Assert.False(tracker.WasInvoked, "Next should NOT be called when message is expired");
        var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
        Assert.True(feature.MessageConsumed, "MessageConsumed should be set for expired messages");
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_DeliverByEqualsCurrentTime()
    {
        // arrange - The middleware checks "DeliverBy.Value < timeProvider.GetUtcNow()"
        // so exact current time should NOT trigger expiry (not strictly less than)
        var baseTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var fakeTime = new FakeTimeProvider(baseTime);
        var middleware = new ReceiveExpiryMiddleware(fakeTime);
        var context = new StubReceiveContext
        {
            DeliverBy = baseTime // exactly equal
        };
        var tracker = new InvocationTracker();
        var next = CreateTrackingDelegate(tracker);

        // act
        await middleware.InvokeAsync(context, next);

        // assert - boundary: not strictly less than, so message is not expired
        Assert.True(tracker.WasInvoked, "Next should be called when DeliverBy equals current time");
    }

    [Fact]
    public async Task InvokeAsync_Should_SetReceiveConsumerFeature_When_Invoked()
    {
        // arrange
        var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var middleware = new ReceiveExpiryMiddleware(fakeTime);
        var context = new StubReceiveContext { DeliverBy = null };
        var next = CreatePassthroughDelegate();

        // act
        await middleware.InvokeAsync(context, next);

        // assert - feature is created/accessed
        var feature = context.Features.TryGet<ReceiveConsumerFeature>(out var f);
        Assert.True(feature, "ReceiveConsumerFeature should be created by the middleware");
        Assert.NotNull(f);
    }

    [Fact]
    public void Create_Should_ReturnValidConfiguration_When_Called()
    {
        // arrange & act
        var configuration = ReceiveExpiryMiddleware.Create();

        // assert
        Assert.NotNull(configuration);
        Assert.Equal("Expiry", configuration.Key);
        Assert.NotNull(configuration.Middleware);
    }
}
