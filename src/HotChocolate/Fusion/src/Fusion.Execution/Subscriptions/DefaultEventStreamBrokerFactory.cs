using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Subscriptions;

public sealed class DefaultEventStreamBrokerFactory(IServiceProvider services) : IEventStreamBrokerFactory
{
    public const string DefaultBrokerKey = "";

    public IEventStreamBroker Create(string? broker)
    {
        var key = broker ?? DefaultBrokerKey;

        try
        {
            return services
                .GetRequiredKeyedService<IEventStreamBrokerProvider>(key)
                .Create();
        }
        catch (InvalidOperationException ex)
        {
            var label = broker is null ? "<default>" : $"'{broker}'";
            throw new InvalidOperationException($"No event stream broker is registered for {label}.", ex);
        }
    }
}
