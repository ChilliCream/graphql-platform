using Microsoft.Extensions.DependencyInjection;

namespace GreenDonut.DependencyInjection;

/// <summary>
/// Represents a registration for a DataLoader.
/// </summary>
public sealed class DataLoaderRegistration
{
    private readonly DataLoaderFactory _factory;

    public DataLoaderRegistration(Type instanceType)
        : this(instanceType, instanceType) { }

    public DataLoaderRegistration(Type serviceType, Type instanceType)
    {
        ServiceType = serviceType;
        InstanceType = instanceType;

        var factory = ActivatorUtilities.CreateFactory(instanceType, []);
        _factory = sp => (IDataLoader)factory.Invoke(sp, null);
    }

    public DataLoaderRegistration(Type serviceType, DataLoaderFactory factory)
        : this(serviceType, serviceType, factory) { }

    public DataLoaderRegistration(Type serviceType, Type instanceType, DataLoaderFactory factory)
    {
        ServiceType = serviceType;
        InstanceType = instanceType;
        _factory = factory;
    }

    /// <summary>
    /// Gets the service type.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the instance type.
    /// </summary>
    public Type InstanceType { get; }

    /// <summary>
    /// Creates a new DataLoader instance.
    /// </summary>
    /// <param name="services">
    /// The available services.
    /// </param>
    /// <returns>
    /// Returns the new DataLoader instance.
    /// </returns>
    public IDataLoader CreateDataLoader(IServiceProvider services)
        => _factory(services);
}
