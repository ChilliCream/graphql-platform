using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mocha.Middlewares;
using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Provides extension methods for configuring the message bus through the host builder, including handlers, sagas, services, and options.
/// </summary>
public static class MessageBusHostBuilderExtensions
{
    /// <summary>
    /// Registers an event handler with the message bus and adds it to the service collection.
    /// </summary>
    /// <typeparam name="THandler">The event handler type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddEventHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IMessageBusHostBuilder builder)
        where THandler : class, IEventHandler
    {
        builder.Services.TryAddScoped<THandler>();
        builder.ConfigureMessageBus(static h => h.AddHandler<THandler>());

        return builder;
    }

    /// <summary>
    /// Registers a batch event handler with the message bus and adds it to the service collection.
    /// </summary>
    /// <typeparam name="THandler">The batch event handler type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">Optional action to configure batch options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddBatchHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(
        this IMessageBusHostBuilder builder,
        Action<BatchOptions>? configure = null)
        where THandler : class, IBatchEventHandler
    {
        builder.Services.TryAddScoped<THandler>();
        builder.ConfigureMessageBus(h => h.AddBatchHandler<THandler>(configure));

        return builder;
    }

    /// <summary>
    /// Registers a saga with the message bus.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddSaga<TSaga>(this IMessageBusHostBuilder builder) where TSaga : Saga, new()
    {
        builder.ConfigureMessageBus(static h => h.AddSaga<TSaga>());
        return builder;
    }

    /// <summary>
    /// Registers a request handler with the message bus and adds it to the service collection.
    /// </summary>
    /// <typeparam name="THandler">The request handler type.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddRequestHandler<THandler>(this IMessageBusHostBuilder builder)
        where THandler : class, IEventRequestHandler
    {
        builder.Services.TryAddScoped<THandler>();
        builder.ConfigureMessageBus(static h => h.AddHandler<THandler>());

        return builder;
    }

    /// <summary>
    /// Registers a consumer with the message bus and adds it to the service collection.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type implementing <see cref="IConsumer"/>.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddConsumer<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConsumer>(
        this IMessageBusHostBuilder builder)
        where TConsumer : class, IConsumer
    {
        builder.Services.TryAddScoped<TConsumer>();
        builder.ConfigureMessageBus(static h => h.AddHandler<TConsumer>());

        return builder;
    }

    /// <summary>
    /// Configures additional services for the message bus through the host builder.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure services.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder ConfigureServices(
        this IMessageBusHostBuilder builder,
        Action<IServiceCollection> configure)
    {
        builder.ConfigureMessageBus(h => h.ConfigureServices(configure));
        return builder;
    }

    /// <summary>
    /// Configures additional services for the message bus through the host builder, with access to the existing service provider.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure services with access to the service provider.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder ConfigureServices(
        this IMessageBusHostBuilder builder,
        Action<IServiceProvider, IServiceCollection> configure)
    {
        builder.ConfigureMessageBus(h => h.ConfigureServices(configure));
        return builder;
    }

    /// <summary>
    /// Registers a message type with custom configuration through the host builder.
    /// </summary>
    /// <typeparam name="TMessage">The message type to register.</typeparam>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure the message type descriptor.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder AddMessage<TMessage>(
        this IMessageBusHostBuilder builder,
        Action<IMessageTypeDescriptor> configure)
        where TMessage : class
    {
        builder.ConfigureMessageBus(h => h.AddMessage<TMessage>(configure));
        return builder;
    }

    /// <summary>
    /// Configures host information through the host builder.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure host information.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder Host(
        this IMessageBusHostBuilder builder,
        Action<IHostInfoDescriptor> configure)
    {
        builder.ConfigureMessageBus(h => h.Host(configure));
        return builder;
    }

    /// <summary>
    /// Modifies messaging options through the host builder.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to modify messaging options.</param>
    /// <returns>The builder for method chaining.</returns>
    public static IMessageBusHostBuilder ModifyOptions(
        this IMessageBusHostBuilder builder,
        Action<MessagingOptions> configure)
    {
        builder.ConfigureMessageBus(h => h.ModifyOptions(configure));
        return builder;
    }

    /// <summary>
    /// Applies a configuration action directly to the underlying message bus builder.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="configure">The action to configure the message bus builder.</param>
    public static void ConfigureMessageBus(this IMessageBusHostBuilder builder, Action<IMessageBusBuilder> configure)
    {
        builder.Configure<MessageBusSetup>(options => options.ConfigureMessageBus.Add(configure));
    }

    private static void Configure<TOptions>(this IMessageBusHostBuilder builder, Action<TOptions> configure)
        where TOptions : class
    {
        builder.Services.Configure(builder.Name, configure);
    }
}
