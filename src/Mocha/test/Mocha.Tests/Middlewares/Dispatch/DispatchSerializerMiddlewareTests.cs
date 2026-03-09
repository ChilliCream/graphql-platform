using Mocha.Middlewares;

namespace Mocha.Tests.Middlewares.Dispatch;

/// <summary>
/// Tests for <see cref="DispatchSerializerMiddleware"/> which ensures outgoing messages are
/// serialized into envelopes before transport dispatch.
/// </summary>
public sealed class DispatchSerializerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_SkipSerialization_When_EnvelopeAlreadySet()
    {
        // arrange
        var middleware = new DispatchSerializerMiddleware();
        var context = new DispatchContext { Envelope = new MessageEnvelope() };
        var nextCalled = false;

        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        await middleware.InvokeAsync(context, next);

        // assert — envelope was pre-set, so serialization is bypassed and next is called
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Should_PreserveExistingEnvelope_When_EnvelopeAlreadySet()
    {
        // arrange
        var middleware = new DispatchSerializerMiddleware();
        var originalEnvelope = new MessageEnvelope { MessageId = "pre-built-1" };
        var context = new DispatchContext { Envelope = originalEnvelope };

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act
        await middleware.InvokeAsync(context, next);

        // assert — the original envelope should be unchanged
        Assert.Same(originalEnvelope, context.Envelope);
        Assert.Equal("pre-built-1", context.Envelope.MessageId);
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowInvalidOperationException_When_MessageIsNull()
    {
        // arrange
        var middleware = new DispatchSerializerMiddleware();
        var context = new DispatchContext { Message = null };

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Contains("body", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowInvalidOperationException_When_MessageTypeIsNull()
    {
        // arrange
        var middleware = new DispatchSerializerMiddleware();
        var context = new DispatchContext { Message = new object(), MessageType = null };

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Contains("message type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowInvalidOperationException_When_ContentTypeIsNull()
    {
        // arrange
        var middleware = new DispatchSerializerMiddleware();
        var context = new DispatchContext
        {
            Message = new object(),
            MessageType = new MessageType(),
            ContentType = null
        };

        DispatchDelegate next = _ => ValueTask.CompletedTask;

        // act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(context, next).AsTask()
        );

        Assert.Contains("content type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotCallNext_When_ValidationFails()
    {
        // arrange
        var middleware = new DispatchSerializerMiddleware();
        var context = new DispatchContext { Message = null };
        var nextCalled = false;

        DispatchDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act
        try
        {
            await middleware.InvokeAsync(context, next);
        }
        catch (InvalidOperationException)
        {
            // expected
        }

        // assert
        Assert.False(nextCalled, "Next should not be called when validation fails");
    }

    [Fact]
    public void Create_Should_ReturnConfiguration_WithCorrectKey()
    {
        // act
        var configuration = DispatchSerializerMiddleware.Create();

        // assert
        Assert.NotNull(configuration);
        Assert.Equal("Serialization", configuration.Key);
        Assert.NotNull(configuration.Middleware);
    }

    [Fact]
    public async Task Create_Should_ProduceWorkingMiddleware_When_EnvelopePreSet()
    {
        // arrange
        var configuration = DispatchSerializerMiddleware.Create();
        var factoryContext = new DispatchMiddlewareFactoryContext
        {
            Services = null!,
            Endpoint = null!,
            Transport = null!
        };
        var nextCalled = false;

        DispatchDelegate terminalNext = _ =>
        {
            nextCalled = true;
            return ValueTask.CompletedTask;
        };

        // act — create the middleware from the factory and invoke with pre-set envelope
        var middlewareDelegate = configuration.Middleware(factoryContext, terminalNext);
        var context = new DispatchContext { Envelope = new MessageEnvelope() };
        await middlewareDelegate(context);

        // assert
        Assert.True(nextCalled);
    }
}
