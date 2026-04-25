using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mocha.Resources;

namespace Mocha;

/// <summary>
/// Provides extension methods on <see cref="IServiceCollection"/> for registering the message bus
/// runtime and its dependencies.
/// </summary>
public static class MessageBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers the message bus runtime, default middleware, and supporting services into the
    /// dependency injection container.
    /// </summary>
    /// <param name="services">
    /// The service collection to register services into.
    /// </param>
    /// <returns>
    /// An <see cref="IMessageBusHostBuilder"/> for further host-level configuration.
    /// </returns>
    public static IMessageBusHostBuilder AddMessageBus(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddScoped<ConsumeContextAccessor>();
        services.AddScoped<IMessageBus, DefaultMessageBus>();

        services.AddSingleton(static sp =>
        {
            var timeProvider = sp.GetService<TimeProvider>() ?? TimeProvider.System;
            return new DeferredResponseManager(timeProvider);
        });
        services.AddPoolingCore();

        services.AddSingleton<IMessagingRuntime>(x =>
        {
            var setups = x.GetRequiredService<IOptions<MessageBusSetup>>();

            var builder = new MessageBusBuilder();

            builder.AddDefaults();

            foreach (var setup in setups.Value.ConfigureMessageBus)
            {
                setup(builder);
            }

            return builder.Build(x);
        });

        services.AddSingleton<IHostedService, MessagingRuntimeHostedService>();

        AddResources(services);

        return new MessageBusHostBuilder(services, string.Empty);
    }

    private static void AddResources(IServiceCollection services)
    {
        services.AddMochaResources();

        services.TryAddSingleton(static sp =>
            new MochaMessageBusResourceSource((MessagingRuntime)sp.GetRequiredService<IMessagingRuntime>()));

        services.AddSingleton<IMochaResourceSourceContributor>(
            static sp => new MochaResourceSourceContributor(sp.GetRequiredService<MochaMessageBusResourceSource>()));

        services.AddSingleton(new MochaResourceDefinition("mocha.service", "Service"));
        services.AddSingleton(new MochaResourceDefinition("mocha.message_type", "Message Type"));
        services.AddSingleton(new MochaResourceDefinition("mocha.handler", "Handler"));
        services.AddSingleton(new MochaResourceDefinition("mocha.inbound_route", "Inbound Route"));
        services.AddSingleton(new MochaResourceDefinition("mocha.outbound_route", "Outbound Route"));
        services.AddSingleton(new MochaResourceDefinition("mocha.transport", "Transport"));
        services.AddSingleton(new MochaResourceDefinition("mocha.receive_endpoint", "Receive Endpoint"));
        services.AddSingleton(new MochaResourceDefinition("mocha.dispatch_endpoint", "Dispatch Endpoint"));
        services.AddSingleton(new MochaResourceDefinition("mocha.saga", "Saga"));
        services.AddSingleton(new MochaResourceDefinition("mocha.saga.state", "Saga State"));
        services.AddSingleton(new MochaResourceDefinition("mocha.saga.transition", "Saga Transition"));
        services.AddSingleton(new MochaResourceDefinition("mocha.queue", "Queue"));
        services.AddSingleton(new MochaResourceDefinition("mocha.exchange", "Exchange"));
        services.AddSingleton(new MochaResourceDefinition("mocha.topic", "Topic"));
        services.AddSingleton(new MochaResourceDefinition("mocha.binding", "Binding"));
        services.AddSingleton(new MochaResourceDefinition("mocha.topology_entity", "Topology Entity"));
    }
}
