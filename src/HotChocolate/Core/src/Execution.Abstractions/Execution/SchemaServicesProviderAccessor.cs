namespace HotChocolate.Execution;

public sealed class SchemaServicesProviderAccessor
{
    public SchemaServicesProviderAccessor(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }

    public IServiceProvider Services { get; }
}
