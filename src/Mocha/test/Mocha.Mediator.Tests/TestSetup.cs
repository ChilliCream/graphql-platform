using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Mediator.Tests;

// ---------------------------------------------------------------------------
// Message types (prefixed with "Dispatch" to avoid collisions with
// MiddlewarePipelineTests types)
// ---------------------------------------------------------------------------

public sealed record DispatchVoidCommand(string Value) : ICommand;

public sealed record DispatchCommand(string Value) : ICommand<DispatchResponse>;

public sealed record DispatchQuery(int Id) : IQuery<DispatchResponse>;

public sealed record DispatchNotification(string Payload) : INotification;

public sealed record DispatchResponse(string Data);

// ---------------------------------------------------------------------------
// Command that always throws
// ---------------------------------------------------------------------------

public sealed record DispatchThrowingCommand(string Value) : ICommand;

// ---------------------------------------------------------------------------
// Command that captures the CancellationToken
// ---------------------------------------------------------------------------

public sealed record DispatchTokenCapturingCommand : ICommand;

// ---------------------------------------------------------------------------
// Async (non-synchronously completing) command
// ---------------------------------------------------------------------------

public sealed record DispatchAsyncCommand(string Value) : ICommand<DispatchResponse>;

// ---------------------------------------------------------------------------
// Handlers
// ---------------------------------------------------------------------------

public sealed class DispatchVoidCommandHandler : ICommandHandler<DispatchVoidCommand>
{
    public static bool WasInvoked { get; set; }

    public ValueTask HandleAsync(DispatchVoidCommand command, CancellationToken cancellationToken)
    {
        WasInvoked = true;
        return default;
    }
}

public sealed class DispatchCommandHandler : ICommandHandler<DispatchCommand, DispatchResponse>
{
    private static readonly DispatchResponse s_cached = new("handled");

    public ValueTask<DispatchResponse> HandleAsync(DispatchCommand command, CancellationToken cancellationToken)
        => new(s_cached);
}

public sealed class DispatchQueryHandler : IQueryHandler<DispatchQuery, DispatchResponse>
{
    private static readonly DispatchResponse s_cached = new("query-result");

    public ValueTask<DispatchResponse> HandleAsync(DispatchQuery query, CancellationToken cancellationToken)
        => new(s_cached);
}

public sealed class DispatchNotificationHandler : INotificationHandler<DispatchNotification>
{
    public static bool WasInvoked { get; set; }

    public ValueTask HandleAsync(DispatchNotification notification, CancellationToken cancellationToken)
    {
        WasInvoked = true;
        return default;
    }
}

public sealed class DispatchSecondNotificationHandler : INotificationHandler<DispatchNotification>
{
    public static bool WasInvoked { get; set; }

    public ValueTask HandleAsync(DispatchNotification notification, CancellationToken cancellationToken)
    {
        WasInvoked = true;
        return default;
    }
}

public sealed class DispatchThrowingCommandHandler : ICommandHandler<DispatchThrowingCommand>
{
    public ValueTask HandleAsync(DispatchThrowingCommand command, CancellationToken cancellationToken)
        => throw new InvalidOperationException("handler-exploded");
}

public sealed class DispatchTokenCapturingHandler : ICommandHandler<DispatchTokenCapturingCommand>
{
    public static CancellationToken CapturedToken { get; set; }

    public ValueTask HandleAsync(DispatchTokenCapturingCommand command, CancellationToken cancellationToken)
    {
        CapturedToken = cancellationToken;
        return default;
    }
}

public sealed class DispatchAsyncCommandHandler : ICommandHandler<DispatchAsyncCommand, DispatchResponse>
{
    public async ValueTask<DispatchResponse> HandleAsync(DispatchAsyncCommand command, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new DispatchResponse("async-result");
    }
}

// ---------------------------------------------------------------------------
// Helper to build a fully-wired IServiceProvider
// ---------------------------------------------------------------------------

public static class DispatchTestHelper
{
    /// <summary>
    /// Creates a service provider with the mediator infrastructure, registering
    /// only the handlers and pipelines specified by the caller.
    /// </summary>
    public static IServiceProvider BuildProvider(Action<IMediatorHostBuilder, IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        var builder = MediatorServiceCollectionExtensions.AddMediator(services);
        configure(builder, services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a service provider with the standard set of test handlers and pipelines.
    /// </summary>
    public static IServiceProvider BuildDefaultProvider()
    {
        return BuildProvider((builder, services) =>
        {
            // Void command
            services.AddScoped<ICommandHandler<DispatchVoidCommand>, DispatchVoidCommandHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchVoidCommand),
                Terminal = PipelineBuilder.BuildVoidCommandTerminal<DispatchVoidCommand>()
            }));

            // Command with response
            services.AddScoped<ICommandHandler<DispatchCommand, DispatchResponse>, DispatchCommandHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchCommand),
                ResponseType = typeof(DispatchResponse),
                Terminal = PipelineBuilder.BuildCommandTerminal<DispatchCommand, DispatchResponse>()
            }));

            // Query
            services.AddScoped<IQueryHandler<DispatchQuery, DispatchResponse>, DispatchQueryHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchQuery),
                ResponseType = typeof(DispatchResponse),
                Terminal = PipelineBuilder.BuildQueryTerminal<DispatchQuery, DispatchResponse>()
            }));

            // Notification - single handler
            services.AddScoped<DispatchNotificationHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchNotification),
                Terminal = PipelineBuilder.BuildNotificationTerminal<DispatchNotification>(
                    new[] { typeof(DispatchNotificationHandler) })
            }));

            // Throwing command
            services.AddScoped<ICommandHandler<DispatchThrowingCommand>, DispatchThrowingCommandHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchThrowingCommand),
                Terminal = PipelineBuilder.BuildVoidCommandTerminal<DispatchThrowingCommand>()
            }));

            // Token capturing command
            services.AddScoped<ICommandHandler<DispatchTokenCapturingCommand>, DispatchTokenCapturingHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchTokenCapturingCommand),
                Terminal = PipelineBuilder.BuildVoidCommandTerminal<DispatchTokenCapturingCommand>()
            }));

            // Async command
            services.AddScoped<ICommandHandler<DispatchAsyncCommand, DispatchResponse>, DispatchAsyncCommandHandler>();
            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchAsyncCommand),
                ResponseType = typeof(DispatchResponse),
                Terminal = PipelineBuilder.BuildCommandTerminal<DispatchAsyncCommand, DispatchResponse>()
            }));
        });
    }

    /// <summary>
    /// Creates a service provider with multiple notification handlers to test fan-out dispatch.
    /// </summary>
    public static IServiceProvider BuildMultiNotificationProvider()
    {
        return BuildProvider((builder, services) =>
        {
            services.AddScoped<DispatchNotificationHandler>();
            services.AddScoped<DispatchSecondNotificationHandler>();

            builder.ConfigureMediator(b => b.RegisterPipeline(new MediatorPipelineConfiguration
            {
                MessageType = typeof(DispatchNotification),
                Terminal = PipelineBuilder.BuildNotificationTerminal<DispatchNotification>(
                    new[] { typeof(DispatchNotificationHandler), typeof(DispatchSecondNotificationHandler) })
            }));
        });
    }
}
