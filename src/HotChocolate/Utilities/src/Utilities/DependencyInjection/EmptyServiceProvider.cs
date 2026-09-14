using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Utilities;

public sealed class EmptyServiceProvider : IServiceProvider, IServiceProviderIsService
{
    private EmptyServiceProvider()
    {
    }

    public object? GetService(Type serviceType) => null;

    public bool IsService(Type serviceType) => false;

    public static EmptyServiceProvider Instance { get; } = new();
}
