// .NET 11 ships Microsoft.Extensions.DependencyInjection.EmptyServiceProvider, so callers
// resolve that type on newer targets instead.
#if !NET11_0_OR_GREATER
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
#endif
