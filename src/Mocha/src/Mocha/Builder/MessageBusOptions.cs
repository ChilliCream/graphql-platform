using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        return new MessageBusHostBuilder(services, string.Empty);
    }
}
