using System.Diagnostics.CodeAnalysis;
using ChilliCream.Hosting.CommandLine;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class HostCommandServiceCollectionExtensions
{
    /// <summary>
    /// Registers a command contributor once per implementation type.
    /// </summary>
    public static IServiceCollection AddHostCommandContributor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IServiceCollection services)
        where T : class, IHostCommandContributor
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IHostCommandContributor, T>());
        return services;
    }
}
