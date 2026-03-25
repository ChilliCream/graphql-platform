using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public class AddHandlerTests
{
    private static IServiceProvider BuildProvider(Action<IMediatorHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMediator();
        configure(builder);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddHandler_Should_DispatchVoidCommand()
    {
        // Arrange
        ManualVoidCommandHandler.WasInvoked = false;
        var sp = BuildProvider(b => b.AddHandler<ManualVoidCommandHandler>());
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new ManualVoidCommand("test"));

        // Assert
        Assert.True(ManualVoidCommandHandler.WasInvoked);
    }

    [Fact]
    public async Task AddHandler_Should_DispatchCommandWithResponse()
    {
        // Arrange
        var sp = BuildProvider(b => b.AddHandler<ManualCommandHandler>());
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new ManualCommand("hello"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("manual-hello", result.Data);
    }

    [Fact]
    public async Task AddHandler_Should_DispatchQuery()
    {
        // Arrange
        var sp = BuildProvider(b => b.AddHandler<ManualQueryHandler>());
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.QueryAsync(new ManualQuery(42));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("query-42", result.Data);
    }

    [Fact]
    public async Task AddHandler_Should_DispatchNotification_When_SingleHandler()
    {
        // Arrange
        ManualNotificationHandler1.WasInvoked = false;
        var sp = BuildProvider(b => b.AddHandler<ManualNotificationHandler1>());
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new ManualNotification("ping"));

        // Assert
        Assert.True(ManualNotificationHandler1.WasInvoked);
    }

    [Fact]
    public async Task AddHandler_Should_DispatchToAllHandlers_When_MultipleNotificationHandlers()
    {
        // Arrange
        ManualNotificationHandler1.WasInvoked = false;
        ManualNotificationHandler2.WasInvoked = false;
        var sp = BuildProvider(b =>
        {
            b.AddHandler<ManualNotificationHandler1>();
            b.AddHandler<ManualNotificationHandler2>();
        });
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.PublishAsync(new ManualNotification("fan-out"));

        // Assert
        Assert.True(ManualNotificationHandler1.WasInvoked);
        Assert.True(ManualNotificationHandler2.WasInvoked);
    }

    [Fact]
    public void AddHandler_Should_Throw_When_TypeIsNotHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddMediator();
        builder.AddHandler<string>();
        using var sp = services.BuildServiceProvider();

        // Act & Assert -- validation is deferred to Build() time, which happens
        // when MediatorRuntime is first resolved (triggered by IMediator resolution).
        Assert.Throws<InvalidOperationException>(
            () => sp.GetRequiredService<IMediator>());
    }

    [Fact]
    public async Task AddHandler_Should_SupportFluentChaining()
    {
        // Arrange
        ManualVoidCommandHandler.WasInvoked = false;
        var sp = BuildProvider(b =>
            b.AddHandler<ManualVoidCommandHandler>()
             .AddHandler<ManualCommandHandler>()
             .AddHandler<ManualQueryHandler>()
             .AddHandler<ManualNotificationHandler1>());
        using var scope = sp.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act & Assert — all handler types work together
        await mediator.SendAsync(new ManualVoidCommand("v"));
        Assert.True(ManualVoidCommandHandler.WasInvoked);

        var cmdResult = await mediator.SendAsync(new ManualCommand("c"));
        Assert.Equal("manual-c", cmdResult.Data);

        var queryResult = await mediator.QueryAsync(new ManualQuery(7));
        Assert.Equal("query-7", queryResult.Data);

        ManualNotificationHandler1.WasInvoked = false;
        await mediator.PublishAsync(new ManualNotification("n"));
        Assert.True(ManualNotificationHandler1.WasInvoked);
    }
}

public sealed record ManualVoidCommand(string Value) : ICommand;

public sealed record ManualCommand(string Value) : ICommand<ManualResponse>;

public sealed record ManualQuery(int Id) : IQuery<ManualResponse>;

public sealed record ManualNotification(string Payload) : INotification;

public sealed record ManualResponse(string Data);

public sealed class ManualVoidCommandHandler : ICommandHandler<ManualVoidCommand>
{
    public static bool WasInvoked { get; set; }

    public ValueTask HandleAsync(ManualVoidCommand command, CancellationToken cancellationToken)
    {
        WasInvoked = true;
        return default;
    }
}

public sealed class ManualCommandHandler : ICommandHandler<ManualCommand, ManualResponse>
{
    public ValueTask<ManualResponse> HandleAsync(ManualCommand command, CancellationToken cancellationToken)
        => new(new ManualResponse("manual-" + command.Value));
}

public sealed class ManualQueryHandler : IQueryHandler<ManualQuery, ManualResponse>
{
    public ValueTask<ManualResponse> HandleAsync(ManualQuery query, CancellationToken cancellationToken)
        => new(new ManualResponse("query-" + query.Id));
}

public sealed class ManualNotificationHandler1 : INotificationHandler<ManualNotification>
{
    public static bool WasInvoked { get; set; }

    public ValueTask HandleAsync(ManualNotification notification, CancellationToken cancellationToken)
    {
        WasInvoked = true;
        return default;
    }
}

public sealed class ManualNotificationHandler2 : INotificationHandler<ManualNotification>
{
    public static bool WasInvoked { get; set; }

    public ValueTask HandleAsync(ManualNotification notification, CancellationToken cancellationToken)
    {
        WasInvoked = true;
        return default;
    }
}
