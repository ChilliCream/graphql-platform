using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

public sealed class MiddlewareFactoryContextTests
{
    [Fact]
    public async Task FactoryContext_Should_ExposeCorrectTypes_When_CommandWithResponse()
    {
        // Arrange
        Type? capturedMessageType = null;
        Type? capturedResponseType = null;

        var mediator = BuildMediator(
            CaptureTypesMiddleware(t => capturedMessageType = t, t => capturedResponseType = t),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.Equal(typeof(PipelineTestCommand), capturedMessageType);
        Assert.Equal(typeof(string), capturedResponseType);
    }

    [Fact]
    public async Task FactoryContext_Should_HaveNullResponseType_When_VoidCommand()
    {
        // Arrange
        Type? capturedMessageType = null;
        Type? capturedResponseType = typeof(object); // sentinel to verify it's set to null

        var mediator = BuildMediator(
            CaptureTypesMiddleware(t => capturedMessageType = t, t => capturedResponseType = t),
            registerVoidCommand: true);

        // Act
        await mediator.SendAsync(new CtxTestVoidCommand());

        // Assert
        Assert.Equal(typeof(CtxTestVoidCommand), capturedMessageType);
        Assert.Null(capturedResponseType);
    }

    [Fact]
    public async Task FactoryContext_Should_ExposeCorrectTypes_When_Query()
    {
        // Arrange
        Type? capturedMessageType = null;
        Type? capturedResponseType = null;

        var mediator = BuildMediator(
            CaptureTypesMiddleware(t => capturedMessageType = t, t => capturedResponseType = t),
            registerQuery: true);

        // Act
        await mediator.QueryAsync(new CtxTestQuery());

        // Assert
        Assert.Equal(typeof(CtxTestQuery), capturedMessageType);
        Assert.Equal(typeof(int), capturedResponseType);
    }

    [Fact]
    public async Task FactoryContext_Should_HaveNullResponseType_When_Notification()
    {
        // Arrange
        Type? capturedMessageType = null;
        Type? capturedResponseType = typeof(object);

        var mediator = BuildMediator(
            CaptureTypesMiddleware(t => capturedMessageType = t, t => capturedResponseType = t),
            registerNotification: true);

        // Act
        await mediator.PublishAsync(new CtxTestNotification());

        // Assert
        Assert.Equal(typeof(CtxTestNotification), capturedMessageType);
        Assert.Null(capturedResponseType);
    }

    [Fact]
    public async Task IsCommand_Should_ReturnTrue_When_VoidCommand()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsCommand()),
            registerVoidCommand: true);

        // Act
        await mediator.SendAsync(new CtxTestVoidCommand());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsCommandWithResponse_Should_ReturnTrue_When_CommandWithResponse()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsCommandWithResponse()),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsQuery_Should_ReturnTrue_When_Query_And_ReturnFalse_When_Command()
    {
        // Arrange
        bool? queryResult = null;
        bool? commandResult = null;

        var queryMiddleware = CheckMiddleware(ctx => queryResult = ctx.IsQuery());
        var commandMiddleware = CheckMiddleware(ctx => commandResult = ctx.IsQuery());

        var mediator1 = BuildMediator(queryMiddleware, registerQuery: true);
        var mediator2 = BuildMediator(commandMiddleware, registerCommand: true);

        // Act
        await mediator1.QueryAsync(new CtxTestQuery());
        await mediator2.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.True(queryResult);
        Assert.False(commandResult);
    }

    [Fact]
    public async Task IsNotification_Should_ReturnTrue_When_Notification()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsNotification()),
            registerNotification: true);

        // Act
        await mediator.PublishAsync(new CtxTestNotification());

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsCommand_Should_ReturnFalse_When_Query()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsCommand()),
            registerQuery: true);

        // Act
        await mediator.QueryAsync(new CtxTestQuery());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsCommandWithResponse_Should_ReturnFalse_When_VoidCommand()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsCommandWithResponse()),
            registerVoidCommand: true);

        // Act
        await mediator.SendAsync(new CtxTestVoidCommand());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsNotification_Should_ReturnFalse_When_Command()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsNotification()),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsMessageAssignableTo_Should_ReturnTrue_When_MatchingGenericType()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsMessageAssignableTo<PipelineTestCommand>()),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMessageAssignableTo_Should_ReturnFalse_When_NonMatchingGenericType()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsMessageAssignableTo<CtxTestVoidCommand>()),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsMessageAssignableTo_Should_ReturnTrue_When_MatchingType()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsMessageAssignableTo(typeof(PipelineTestCommand))),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsResponseAssignableTo_Should_ReturnTrue_When_MatchingType()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsResponseAssignableTo(typeof(string))),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsResponseAssignableTo_Should_ReturnFalse_When_NonMatchingType()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsResponseAssignableTo(typeof(int))),
            registerCommand: true);

        // Act
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsResponseAssignableTo_Should_ReturnFalse_When_VoidCommand()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsResponseAssignableTo<string>()),
            registerVoidCommand: true);

        // Act
        await mediator.SendAsync(new CtxTestVoidCommand());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsResponseAssignableTo_Should_ReturnFalse_When_Notification()
    {
        // Arrange
        bool? result = null;

        var mediator = BuildMediator(
            CheckMiddleware(ctx => result = ctx.IsResponseAssignableTo<string>()),
            registerNotification: true);

        // Act
        await mediator.PublishAsync(new CtxTestNotification());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Use_Should_SkipMiddleware_When_MessageTypeDoesNotMatch()
    {
        // Arrange
        var middlewareExecuted = false;

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<PipelineTestCommandHandler>();
        services.AddScoped<CtxTestVoidCommandHandler>();

        // Middleware that only applies to void commands
        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) =>
            {
                if (!factoryCtx.IsCommand() || factoryCtx.IsCommandWithResponse())
                    return next; // Opt out at compile time

                return ctx =>
                {
                    middlewareExecuted = true;
                    return next(ctx);
                };
            },
            "VoidCommandOnly"));

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<PipelineTestCommandHandler>();
            b.AddHandler<CtxTestVoidCommandHandler>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act - send command with response (should NOT trigger middleware)
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.False(middlewareExecuted);

        // Act - send void command (should trigger middleware)
        await mediator.SendAsync(new CtxTestVoidCommand());

        // Assert
        Assert.True(middlewareExecuted);
    }

    [Fact]
    public async Task Use_Should_SkipMiddleware_When_ResponseTypeDoesNotMatch()
    {
        // Arrange
        var middlewareExecuted = false;

        var services = new ServiceCollection();
        var builder = services.AddMediator();

        services.AddScoped<PipelineTestCommandHandler>();
        services.AddScoped<CtxTestQueryHandler>();

        // Middleware that only applies when response is int
        builder.Use(new MediatorMiddlewareConfiguration(
            (factoryCtx, next) =>
            {
                if (!factoryCtx.IsResponseAssignableTo<int>())
                    return next;

                return ctx =>
                {
                    middlewareExecuted = true;
                    return next(ctx);
                };
            },
            "IntResponseOnly"));

        builder.ConfigureMediator(b =>
        {
            b.AddHandler<PipelineTestCommandHandler>();
            b.AddHandler<CtxTestQueryHandler>();
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Act - send command with string response (should NOT trigger)
        await mediator.SendAsync<string>(new PipelineTestCommand("test"));

        // Assert
        Assert.False(middlewareExecuted);

        // Act - send query with int response (should trigger)
        await mediator.QueryAsync(new CtxTestQuery());

        // Assert
        Assert.True(middlewareExecuted);
    }

    /// <summary>
    /// Creates a middleware that captures MessageType and ResponseType from the factory context.
    /// </summary>
    private static MediatorMiddlewareConfiguration CaptureTypesMiddleware(
        Action<Type> onMessageType,
        Action<Type?> onResponseType)
        => new(
            (factoryCtx, next) =>
            {
                onMessageType(factoryCtx.MessageType);
                onResponseType(factoryCtx.ResponseType);
                return next;
            });

    /// <summary>
    /// Creates a middleware that runs a check on the factory context during compilation.
    /// </summary>
    private static MediatorMiddlewareConfiguration CheckMiddleware(
        Action<MediatorMiddlewareFactoryContext> check)
        => new(
            (factoryCtx, next) =>
            {
                check(factoryCtx);
                return next;
            });

    private static IMediator BuildMediator(
        MediatorMiddlewareConfiguration middleware,
        bool registerCommand = false,
        bool registerVoidCommand = false,
        bool registerQuery = false,
        bool registerNotification = false)
    {
        var services = new ServiceCollection();
        var builder = services.AddMediator();

        builder.Use(middleware);

        if (registerCommand)
        {
            services.AddScoped<PipelineTestCommandHandler>();
            builder.ConfigureMediator(b => b.AddHandler<PipelineTestCommandHandler>());
        }

        if (registerVoidCommand)
        {
            services.AddScoped<CtxTestVoidCommandHandler>();
            builder.ConfigureMediator(b => b.AddHandler<CtxTestVoidCommandHandler>());
        }

        if (registerQuery)
        {
            services.AddScoped<CtxTestQueryHandler>();
            builder.ConfigureMediator(b => b.AddHandler<CtxTestQueryHandler>());
        }

        if (registerNotification)
        {
            services.AddScoped<CtxTestNotificationHandler>();
            builder.ConfigureMediator(b => b.AddHandler<CtxTestNotificationHandler>());
        }

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMediator>();
    }
}

public sealed record CtxTestVoidCommand : ICommand;

public sealed class CtxTestVoidCommandHandler : ICommandHandler<CtxTestVoidCommand>
{
    public ValueTask HandleAsync(CtxTestVoidCommand command, CancellationToken cancellationToken)
        => default;
}

public sealed record CtxTestQuery : IQuery<int>;

public sealed class CtxTestQueryHandler : IQueryHandler<CtxTestQuery, int>
{
    public ValueTask<int> HandleAsync(CtxTestQuery query, CancellationToken cancellationToken)
        => new(42);
}

public sealed record CtxTestNotification : INotification;

public sealed class CtxTestNotificationHandler : INotificationHandler<CtxTestNotification>
{
    public ValueTask HandleAsync(CtxTestNotification notification, CancellationToken cancellationToken)
        => default;
}
