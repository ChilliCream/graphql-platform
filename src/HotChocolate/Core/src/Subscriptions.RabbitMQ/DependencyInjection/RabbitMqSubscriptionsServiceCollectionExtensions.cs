using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions.RabbitMQ.Configuration;
using HotChocolate.Subscriptions.RabbitMQ.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace HotChocolate.Subscriptions.RabbitMQ.DependencyInjection;

public static class RabbitMqSubscriptionsServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqSubscribtions(
        this IServiceCollection services,
        Func<IServiceProvider, IConnection> connection,
        Action<Config>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        return services.AddRabbitMqSubscribtions(p => connection(p).CreateModel(), configure);
    }

    public static IServiceCollection AddRabbitMqSubscribtions(
        this IServiceCollection services,
        Func<IServiceProvider, IModel> channel,
        Action<Config>? configure = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (channel is null)
            throw new ArgumentNullException(nameof(channel));

        Config config = new();
        configure?.Invoke(config);

        services.TryAddSingleton<ISerializer, JsonSerializer>();
        services.TryAddSingleton<IExchangeNameFactory, ExchangeNameFactory>();
        services.TryAddSingleton<IQueueNameFactory, QueueNameFactory>();

        services.TryAddSingleton(sp =>
            ActivatorUtilities.CreateInstance<RabbitMqPubSub>(sp, channel(sp), config));
        services.TryAddSingleton<ITopicEventSender>(sp =>
            sp.GetRequiredService<RabbitMqPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(sp =>
            sp.GetRequiredService<RabbitMqPubSub>());

        return services;
    }

    public static IRequestExecutorBuilder AddRabbitMqSubscribtions(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IConnection> connection,
        Action<Config>? configure = null)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (connection is null)
            throw new ArgumentNullException(nameof(connection));

        AddRabbitMqSubscribtions(builder.Services, connection, configure);
        return builder;
    }

    public static IRequestExecutorBuilder AddRabbitMqSubscribtions(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IModel> channel,
        Action<Config>? configure = null)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (channel is null)
            throw new ArgumentNullException(nameof(channel));

        AddRabbitMqSubscribtions(builder.Services, channel, configure);
        return builder;
    }
}
