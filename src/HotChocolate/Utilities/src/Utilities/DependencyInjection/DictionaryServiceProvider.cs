using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Utilities;

public sealed class DictionaryServiceProvider : IServiceProvider, IServiceProviderIsService
{
    private readonly Dictionary<Type, object> _services;

    public DictionaryServiceProvider(Type service, object instance)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(instance);

        _services = new Dictionary<Type, object> { { service, instance }, };
        _services[typeof(IServiceProviderIsService)] = this;
    }

    public DictionaryServiceProvider(params KeyValuePair<Type, object>[] services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services.ToDictionary(t => t.Key, t => t.Value);
        _services[typeof(IServiceProviderIsService)] = this;
    }

    public DictionaryServiceProvider(params (Type, object)[] services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services.ToDictionary(t => t.Item1, t => t.Item2);
        _services[typeof(IServiceProviderIsService)] = this;
    }

    public DictionaryServiceProvider(IEnumerable<KeyValuePair<Type, object>> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services.ToDictionary(t => t.Key, t => t.Value);
        _services[typeof(IServiceProviderIsService)] = this;
    }

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var service) ? service : null;

    public bool IsService(Type serviceType)
        => _services.ContainsKey(serviceType);
}
