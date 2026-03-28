using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class InstrumentationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly TestDiagnosticListener _listener = new();

    public InstrumentationTests()
    {
        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<InstrumentedCommandHandler>();
        services.AddScoped<InstrumentedThrowingCommandHandler>();

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<InstrumentedCommandHandler>();
            b.AddHandler<InstrumentedThrowingCommandHandler>();
        });

        // Register test listener via the builder's internal services.
        builder.AddDiagnosticEventListener(_listener);

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_Should_InvokeDiagnosticListener_When_HandlerSucceeds()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        _listener.Reset();

        // Act
        await mediator.SendAsync(new InstrumentedCommand("test-value"));

        // Assert
        Assert.True(_listener.ExecuteCalled);
        Assert.Equal(typeof(InstrumentedCommand), _listener.CapturedMessageType);
        Assert.IsType<InstrumentedCommand>(_listener.CapturedMessage);
        Assert.True(_listener.ScopeDisposed);
    }

    [Fact]
    public async Task SendAsync_Should_InvokeDiagnosticListenerWithError_When_HandlerThrows()
    {
        // Arrange
        using var scope = _provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        _listener.Reset();

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new InstrumentedThrowingCommand("boom")).AsTask());

        // Assert
        Assert.True(_listener.ExecuteCalled);
        Assert.True(_listener.ExecutionErrorCalled);
        Assert.Same(ex, _listener.CapturedException);
        Assert.Equal(typeof(InstrumentedThrowingCommand), _listener.ErrorMessageType);
        Assert.True(_listener.ScopeDisposed);
    }

    [Fact]
    public async Task SendAsync_Should_InvokeAllListeners_When_MultipleListenersRegistered()
    {
        // Arrange
        var first = new TestDiagnosticListener();
        var second = new SecondTestDiagnosticListener();

        var services = new ServiceCollection();
        var builder = services.AddMediator();
        services.AddScoped<InstrumentedCommandHandler>();

        builder.ConfigureMediator(b => b.AddHandler<InstrumentedCommandHandler>());

        builder.AddDiagnosticEventListener(first);
        builder.AddDiagnosticEventListener(second);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        first.Reset();
        second.Reset();

        // Act
        await mediator.SendAsync(new InstrumentedCommand("multi"));

        // Assert
        Assert.True(first.ExecuteCalled);
        Assert.True(first.ScopeDisposed);
        Assert.True(second.ExecuteCalled);
        Assert.True(second.ScopeDisposed);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }
}

public sealed record InstrumentedCommand(string Value) : ICommand;

public sealed record InstrumentedThrowingCommand(string Value) : ICommand;

public sealed class InstrumentedCommandHandler : ICommandHandler<InstrumentedCommand>
{
    public ValueTask HandleAsync(InstrumentedCommand command, CancellationToken cancellationToken)
        => default;
}

public sealed class InstrumentedThrowingCommandHandler : ICommandHandler<InstrumentedThrowingCommand>
{
    public ValueTask HandleAsync(InstrumentedThrowingCommand command, CancellationToken cancellationToken)
        => throw new InvalidOperationException("instrumented-error");
}

public sealed class TestDiagnosticListener : MediatorDiagnosticEventListener
{
    public bool ExecuteCalled { get; private set; }
    public bool ExecutionErrorCalled { get; private set; }
    public bool ScopeDisposed { get; private set; }
    public Type? CapturedMessageType { get; private set; }
    public object? CapturedMessage { get; private set; }
    public Type? ErrorMessageType { get; private set; }
    public Exception? CapturedException { get; private set; }

    public void Reset()
    {
        ExecuteCalled = false;
        ExecutionErrorCalled = false;
        ScopeDisposed = false;
        CapturedMessageType = null;
        CapturedMessage = null;
        ErrorMessageType = null;
        CapturedException = null;
    }

    public override IDisposable Execute(Type messageType, Type? responseType, object message)
    {
        ExecuteCalled = true;
        CapturedMessageType = messageType;
        CapturedMessage = message;
        return new CallbackDisposable(() => ScopeDisposed = true);
    }

    public override void ExecutionError(Type messageType, Type? responseType, object message, Exception exception)
    {
        ExecutionErrorCalled = true;
        ErrorMessageType = messageType;
        CapturedException = exception;
    }

    private sealed class CallbackDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}

public sealed class SecondTestDiagnosticListener : MediatorDiagnosticEventListener
{
    public bool ExecuteCalled { get; private set; }
    public bool ScopeDisposed { get; private set; }

    public void Reset()
    {
        ExecuteCalled = false;
        ScopeDisposed = false;
    }

    public override IDisposable Execute(Type messageType, Type? responseType, object message)
    {
        ExecuteCalled = true;
        return new CallbackDisposable(() => ScopeDisposed = true);
    }

    private sealed class CallbackDisposable(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
