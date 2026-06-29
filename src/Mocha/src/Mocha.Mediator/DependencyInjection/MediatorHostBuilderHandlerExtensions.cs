using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Mediator;

/// <summary>
/// Provides extension methods for registering mediator handlers through the host builder.
/// </summary>
public static class MediatorHostBuilderHandlerExtensions
{
    /// <summary>
    /// Registers a handler with the mediator using descriptor-based configuration.
    /// The handler type is inspected for <see cref="ICommandHandler{TCommand}"/>,
    /// <see cref="ICommandHandler{TCommand, TResponse}"/>,
    /// <see cref="IQueryHandler{TQuery, TResponse}"/>, or
    /// <see cref="INotificationHandler{TNotification}"/> interfaces
    /// and the appropriate pipeline is configured automatically.
    /// </summary>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="builder">The mediator host builder.</param>
    /// <param name="configure">An optional action to configure the handler descriptor.</param>
    [RequiresDynamicCode("Use source-generated AddHandlerConfiguration for AOT compatibility.")]
    [RequiresUnreferencedCode("Use source-generated AddHandlerConfiguration for AOT compatibility.")]
    public static IMediatorHostBuilder AddHandler<THandler>(
        this IMediatorHostBuilder builder,
        Action<IMediatorHandlerDescriptor>? configure = null)
        where THandler : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAdd(new ServiceDescriptor(
            typeof(THandler), typeof(THandler), builder.Options.ServiceLifetime));

        builder.ConfigureMediator(b => b.AddHandler<THandler>(configure));

        return builder;
    }

    /// <summary>
    /// Registers a handler with the mediator using a pre-built configuration.
    /// This method is intended for use by source-generated code.
    /// </summary>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="builder">The mediator host builder.</param>
    /// <param name="configuration">The pre-built handler configuration.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IMediatorHostBuilder AddHandlerConfiguration<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IMediatorHostBuilder builder,
        MediatorHandlerConfiguration configuration)
        where THandler : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.TryAdd(new ServiceDescriptor(
            typeof(THandler), typeof(THandler), builder.Options.ServiceLifetime));

        builder.ConfigureMediator(b => b.AddHandlerConfiguration(configuration));

        return builder;
    }
}
