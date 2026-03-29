using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class NotificationStrategyTests
{
    [Fact]
    public async Task PublishAsync_Should_InvokeHandlersSequentially_When_UsingForeachAwait()
    {
        // Arrange
        var log = new List<string>();

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped(
            _ => new SequentialHandler1(log));
        services.AddScoped(
            _ => new SequentialHandler2(log));
        services.AddScoped(
            _ => new SequentialHandler3(log));

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<SequentialHandler1>();
            b.AddHandler<SequentialHandler2>();
            b.AddHandler<SequentialHandler3>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new StrategyTestNotification("sequential"));

        // Assert
        Assert.Equal(
            new[] { "Handler1:sequential", "Handler2:sequential", "Handler3:sequential" },
            log);
    }

    [Fact]
    public async Task PublishAsync_Should_StopExecution_When_ForeachAwaitHandlerThrows()
    {
        // Arrange
        var log = new List<string>();

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped(
            _ => new SequentialHandler1(log));
        services.AddScoped(
            _ => new StrategyThrowingHandler());
        services.AddScoped(
            _ => new SequentialHandler3(log));

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<SequentialHandler1>();
            b.AddHandler<StrategyThrowingHandler>();
            b.AddHandler<SequentialHandler3>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new StrategyTestNotification("fail")).AsTask());

        // Handler1 ran, then the throw occurred; Handler3 was never reached
        Assert.Single(log);
        Assert.Equal("Handler1:fail", log[0]);
    }

    [Fact]
    public async Task PublishAsync_Should_InvokeAllHandlers_When_UsingTaskWhenAll()
    {
        // Arrange
        var bag = new ConcurrentBag<string>();

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        builder.ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent);

        services.AddScoped(
            _ => new ConcurrentHandler1(bag));
        services.AddScoped(
            _ => new ConcurrentHandler2(bag));
        services.AddScoped(
            _ => new ConcurrentHandler3(bag));

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<ConcurrentHandler1>();
            b.AddHandler<ConcurrentHandler2>();
            b.AddHandler<ConcurrentHandler3>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new StrategyTestNotification("concurrent"));

        // Assert - all handlers invoked (order is not guaranteed with WhenAll)
        var results = bag.OrderBy(x => x).ToList();
        Assert.Equal(3, results.Count);
        Assert.Contains("Concurrent1:concurrent", results);
        Assert.Contains("Concurrent2:concurrent", results);
        Assert.Contains("Concurrent3:concurrent", results);
    }

    [Fact]
    public async Task PublishAsync_Should_PropagateException_When_TaskWhenAllHandlerThrows()
    {
        // Arrange
        var bag = new ConcurrentBag<string>();

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        builder.ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent);

        services.AddScoped(
            _ => new ConcurrentHandler1(bag));
        services.AddScoped(
            _ => new StrategyThrowingHandler());

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<ConcurrentHandler1>();
            b.AddHandler<StrategyThrowingHandler>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert — concurrent mode surfaces AggregateException with all failures
        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => mediator.PublishAsync(new StrategyTestNotification("throw")).AsTask());

        Assert.Single(ex.InnerExceptions);
        Assert.Equal("notification handler error", ex.InnerExceptions[0].Message);
    }
}

public sealed record StrategyTestNotification(string Value) : INotification;

public sealed class SequentialHandler1(List<string> log) : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        log.Add($"Handler1:{notification.Value}");
        return default;
    }
}

public sealed class SequentialHandler2(List<string> log) : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        log.Add($"Handler2:{notification.Value}");
        return default;
    }
}

public sealed class SequentialHandler3(List<string> log) : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        log.Add($"Handler3:{notification.Value}");
        return default;
    }
}

public sealed class ConcurrentHandler1(ConcurrentBag<string> bag) : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        bag.Add($"Concurrent1:{notification.Value}");
        return default;
    }
}

public sealed class ConcurrentHandler2(ConcurrentBag<string> bag) : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        bag.Add($"Concurrent2:{notification.Value}");
        return default;
    }
}

public sealed class ConcurrentHandler3(ConcurrentBag<string> bag) : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        bag.Add($"Concurrent3:{notification.Value}");
        return default;
    }
}

public sealed class StrategyThrowingHandler : INotificationHandler<StrategyTestNotification>
{
    public ValueTask HandleAsync(StrategyTestNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("notification handler error");
    }
}
