namespace HotChocolate.Fusion.Configuration;

internal sealed class RootServiceProviderAccessor : IRootServiceProviderAccessor
{
    public RootServiceProviderAccessor(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }
}
