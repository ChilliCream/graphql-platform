using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class RootServiceProviderAccessor : IRootServiceProviderAccessor
{
    public RootServiceProviderAccessor(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }
}
