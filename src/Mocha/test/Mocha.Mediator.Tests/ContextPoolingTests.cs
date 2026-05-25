using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class ContextPoolingTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public ContextPoolingTests()
    {
        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<PoolTestCommandHandler>();
        services.AddScoped<PoolTestCommandWithResponseHandler>();
        services.AddScoped<NestedOuterCommandHandler>();
        services.AddScoped<ContextCaptureCommandHandler>();

        // Register middleware that captures context references for nested-dispatch test
        // and context-field verification test. Since Use() applies to all pipelines,
        // both NestedOuterCommand and PoolTestCommand pipelines get this middleware.
        builder.Use(new MediatorMiddlewareConfiguration(
            (_, next) =>
            {
                return ctx =>
                {
                    // Capture for nested dispatch test
                    if (ctx.Message is NestedOuterCommand)
                    {
                        NestedOuterCommandHandler.OuterContextRef = ctx;
                    }
                    else if (ctx.Message is PoolTestCommand
                             && NestedOuterCommandHandler.OuterContextRef is not null
                             && NestedOuterCommandHandler.InnerContextRef is null)
                    {
                        NestedOuterCommandHandler.InnerContextRef = ctx;
                    }

                    // Capture for context-field verification test
                    if (ctx.Message is ContextCaptureCommand)
                    {
                        ContextCapture.CapturedServices = ctx.Services;
                        ContextCapture.CapturedMessage = ctx.Message;
                        ContextCapture.CapturedMessageType = ctx.MessageType;
                        ContextCapture.CapturedCancellationToken = ctx.CancellationToken;
                    }

                    return next(ctx);
                };
            },
            "TestCapture"));

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<PoolTestCommandHandler>();
            b.AddHandler<PoolTestCommandWithResponseHandler>();
            b.AddHandler<NestedOuterCommandHandler>();
            b.AddHandler<ContextCaptureCommandHandler>();
        });

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_Should_IsolateContextPerThread_When_DispatchedConcurrently()
    {
        // Arrange: dispatch from multiple threads simultaneously and capture the
        // response to verify no cross-contamination of Message/Result.

        const int threadCount = 8;
        var barrier = new Barrier(threadCount);
        var capturedMessages = new string[threadCount];
        var tasks = new Task[threadCount];

        for (var i = 0; i < threadCount; i++)
        {
            var index = i;
            tasks[i] = Task.Factory.StartNew(async () =>
            {
                using var scope = _provider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Synchronize all threads to maximize contention
                barrier.SignalAndWait();

                var result = await mediator.SendAsync(
                    new PoolTestCommandWithResponse($"thread-{index}"));
                capturedMessages[index] = result;
            }, TaskCreationOptions.LongRunning).Unwrap();
        }

        await Task.WhenAll(tasks);

        // Assert: each thread got its own message value without cross-contamination.
        for (var i = 0; i < threadCount; i++)
        {
            Assert.Equal($"thread-{i}", capturedMessages[i]);
        }
    }

    [Fact]
    public async Task SendAsync_Should_UseDifferentContext_When_DispatchedFromInsideHandler()
    {
        // Reset the captured references from any prior test runs.
        NestedOuterCommandHandler.OuterContextRef = null;
        NestedOuterCommandHandler.InnerContextRef = null;

        // Arrange: the NestedOuterCommand handler dispatches PoolTestCommand internally.
        // The outer context is still rented, so the inner dispatch must get a different context.
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new NestedOuterCommand(mediator));

        // Assert: two distinct context references were captured.
        Assert.NotNull(NestedOuterCommandHandler.OuterContextRef);
        Assert.NotNull(NestedOuterCommandHandler.InnerContextRef);
        Assert.NotSame(
            NestedOuterCommandHandler.OuterContextRef,
            NestedOuterCommandHandler.InnerContextRef);
    }

    [Fact]
    public async Task SendAsync_Should_PopulateContextFields_When_PipelineExecutes()
    {
        // Arrange
        ContextCapture.Reset();
        using var cts = new CancellationTokenSource();
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new ContextCaptureCommand("payload");

        // Act
        await mediator.SendAsync(command, cts.Token);

        // Assert: the capturing middleware recorded the correct context fields.
        Assert.Same(scope.ServiceProvider, ContextCapture.CapturedServices);
        Assert.Same(command, ContextCapture.CapturedMessage);
        Assert.Equal(typeof(ContextCaptureCommand), ContextCapture.CapturedMessageType);
        Assert.Equal(cts.Token, ContextCapture.CapturedCancellationToken);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}

public sealed record PoolTestCommand(string Value) : ICommand;

public sealed record PoolTestCommandWithResponse(string Value) : ICommand<string>;

public sealed record NestedOuterCommand(IMediator Mediator) : ICommand;

public sealed record ContextCaptureCommand(string Value) : ICommand<string>;

public sealed class PoolTestCommandHandler : ICommandHandler<PoolTestCommand>
{
    public ValueTask HandleAsync(PoolTestCommand command, CancellationToken cancellationToken)
        => default;
}

public sealed class PoolTestCommandWithResponseHandler : ICommandHandler<PoolTestCommandWithResponse, string>
{
    public ValueTask<string> HandleAsync(PoolTestCommandWithResponse command, CancellationToken cancellationToken)
        => new(command.Value);
}

public sealed class NestedOuterCommandHandler : ICommandHandler<NestedOuterCommand>
{
    public static object? OuterContextRef;
    public static object? InnerContextRef;

    public async ValueTask HandleAsync(NestedOuterCommand command, CancellationToken cancellationToken)
    {
        // Outer context is still rented. Inner dispatch must get a different context.
        await command.Mediator.SendAsync(new PoolTestCommand("inner"));
    }
}

public sealed class ContextCaptureCommandHandler : ICommandHandler<ContextCaptureCommand, string>
{
    public ValueTask<string> HandleAsync(ContextCaptureCommand command, CancellationToken cancellationToken)
        => new("captured");
}

/// <summary>
/// Static holder for context fields captured by middleware during pipeline execution.
/// </summary>
internal static class ContextCapture
{
    public static IServiceProvider? CapturedServices;
    public static object? CapturedMessage;
    public static Type? CapturedMessageType;
    public static CancellationToken CapturedCancellationToken;

    public static void Reset()
    {
        CapturedServices = null;
        CapturedMessage = null;
        CapturedMessageType = null;
        CapturedCancellationToken = default;
    }
}
