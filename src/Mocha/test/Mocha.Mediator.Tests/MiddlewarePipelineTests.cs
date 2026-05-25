using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class MiddlewarePipelineTests : IDisposable
{
    private readonly ServiceProvider _provider;

    public MiddlewarePipelineTests()
    {
        var services = new ServiceCollection();

        var builder = services.AddMediator();

        services.AddScoped<PipelineTestCommandHandler>();

        builder.ConfigureMediator(b => b.AddHandler<PipelineTestCommandHandler>());

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_Should_ExecuteMiddlewareInRegistrationOrder_When_MultipleMiddlewareRegistered()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();

        var builder = services.AddMediator();

        services.AddScoped<PipelineTestCommandHandler>();

        builder
            .Use(new MediatorMiddlewareConfiguration(
                (factoryCtx, next) => ctx =>
                {
                    log.Add("MW1-pre");
                    var task = next(ctx);
                    if (task.IsCompletedSuccessfully)
                    {
                        log.Add("MW1-post");
                        return default;
                    }

                    return Awaited(task, log);

                    static async ValueTask Awaited(ValueTask t, List<string> l)
                    {
                        await t.ConfigureAwait(false);
                        l.Add("MW1-post");
                    }
                },
                "MW1"))
            .Use(new MediatorMiddlewareConfiguration(
                (factoryCtx, next) => ctx =>
                {
                    log.Add("MW2-pre");
                    var task = next(ctx);
                    if (task.IsCompletedSuccessfully)
                    {
                        log.Add("MW2-post");
                        return default;
                    }

                    return Awaited(task, log);

                    static async ValueTask Awaited(ValueTask t, List<string> l)
                    {
                        await t.ConfigureAwait(false);
                        l.Add("MW2-post");
                    }
                },
                "MW2"))
            .Use(new MediatorMiddlewareConfiguration(
                (factoryCtx, next) => ctx =>
                {
                    log.Add("MW3-pre");
                    var task = next(ctx);
                    if (task.IsCompletedSuccessfully)
                    {
                        log.Add("MW3-post");
                        return default;
                    }

                    return Awaited(task, log);

                    static async ValueTask Awaited(ValueTask t, List<string> l)
                    {
                        await t.ConfigureAwait(false);
                        l.Add("MW3-post");
                    }
                },
                "MW3"));

        builder.ConfigureMediator(b => b.AddHandler<PipelineTestCommandHandler>());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new PipelineTestCommand("order-test"));

        // Assert
        Assert.Equal("handled:order-test", result);
        Assert.Equal(
            new[] { "MW1-pre", "MW2-pre", "MW3-pre", "MW3-post", "MW2-post", "MW1-post" },
            log);
    }

    [Fact]
    public async Task SendAsync_Should_ExposeContextProperties_When_MiddlewareReadsContext()
    {
        // Arrange
        object? capturedMessage = null;
        Type? capturedMessageType = null;
        CancellationToken capturedToken = default;

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<PipelineTestCommandHandler>();

        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) => ctx =>
            {
                capturedMessage = ctx.Message;
                capturedMessageType = ctx.MessageType;
                capturedToken = ctx.CancellationToken;
                return next(ctx);
            },
            "ContextReader"));

        builder.ConfigureMediator(b => b.AddHandler<PipelineTestCommandHandler>());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        using var cts = new CancellationTokenSource();
        var command = new PipelineTestCommand("context-check");

        // Act
        await mediator.SendAsync(command, cts.Token);

        // Assert
        Assert.Same(command, capturedMessage);
        Assert.Equal(typeof(PipelineTestCommand), capturedMessageType);
        Assert.Equal(cts.Token, capturedToken);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnModifiedResult_When_MiddlewareModifiesResultAfterNext()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<PipelineTestCommandHandler>();

        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) => ctx =>
            {
                var task = next(ctx);
                if (task.IsCompletedSuccessfully)
                {
                    ctx.Result = (string)ctx.Result! + "-modified";
                    return default;
                }

                return Awaited(task, ctx);

                static async ValueTask Awaited(ValueTask t, IMediatorContext c)
                {
                    await t.ConfigureAwait(false);
                    c.Result = (string)c.Result! + "-modified";
                }
            },
            "ResultModifier"));

        builder.ConfigureMediator(b => b.AddHandler<PipelineTestCommandHandler>());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new PipelineTestCommand("test"));

        // Assert
        Assert.Equal("handled:test-modified", result);
    }

    [Fact]
    public async Task SendAsync_Should_PropagateExceptionThroughMiddleware_When_HandlerThrows()
    {
        // Arrange
        var middlewareSawException = false;

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<PipelineThrowingHandler>();

        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) => async ctx =>
            {
                try
                {
                    await next(ctx).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    middlewareSawException = true;
                    throw;
                }
            },
            "ExceptionObserver"));

        builder.ConfigureMediator(b => b.AddHandler<PipelineThrowingHandler>());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new PipelineTestCommand("boom")).AsTask());

        Assert.True(middlewareSawException);
    }

    [Fact]
    public async Task SendAsync_Should_PropagateExceptionAndSkipHandler_When_MiddlewareThrows()
    {
        // Arrange
        var handlerInvoked = false;

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped(
            _ => new PipelineTrackingHandler(() => handlerInvoked = true));

        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) => ctx =>
                throw new ApplicationException("middleware failure"),
            "FailingMiddleware"));

        builder.ConfigureMediator(b => b.AddHandler<PipelineTrackingHandler>());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ApplicationException>(
            () => mediator.SendAsync(new PipelineTestCommand("never-handled")).AsTask());

        Assert.Equal("middleware failure", ex.Message);
        Assert.False(handlerInvoked);
    }

    [Fact]
    public async Task SendAsync_Should_ReturnShortCircuitResult_When_MiddlewareSkipsNext()
    {
        // Arrange
        var handlerInvoked = false;

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped(
            _ => new PipelineTrackingHandler(() => handlerInvoked = true));

        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) => ctx =>
            {
                ctx.Result = "short-circuited";
                return default;
            },
            "ShortCircuit"));

        builder.ConfigureMediator(b => b.AddHandler<PipelineTrackingHandler>());

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new PipelineTestCommand("ignored"));

        // Assert
        Assert.Equal("short-circuited", result);
        Assert.False(handlerInvoked);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}

public sealed record PipelineTestCommand(string Value) : ICommand<string>;

public sealed class PipelineTestCommandHandler : ICommandHandler<PipelineTestCommand, string>
{
    public ValueTask<string> HandleAsync(PipelineTestCommand command, CancellationToken cancellationToken)
    {
        return new ValueTask<string>($"handled:{command.Value}");
    }
}

public sealed class PipelineThrowingHandler : ICommandHandler<PipelineTestCommand, string>
{
    public ValueTask<string> HandleAsync(PipelineTestCommand command, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("handler error");
    }
}

public sealed class PipelineTrackingHandler : ICommandHandler<PipelineTestCommand, string>
{
    private readonly Action _onInvoked;

    public PipelineTrackingHandler(Action onInvoked)
    {
        _onInvoked = onInvoked;
    }

    public ValueTask<string> HandleAsync(PipelineTestCommand command, CancellationToken cancellationToken)
    {
        _onInvoked();
        return new ValueTask<string>($"handled:{command.Value}");
    }
}
