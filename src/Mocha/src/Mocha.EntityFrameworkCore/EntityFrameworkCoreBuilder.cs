using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Configures Entity Framework Core messaging features for a DbContext type.
/// </summary>
internal sealed class EntityFrameworkCoreBuilder<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TContext>(
    IMessageBusHostBuilder hostBuilder) : IEntityFrameworkCoreBuilder
    where TContext : DbContext
{
    /// <inheritdoc />
    public IServiceCollection Services => hostBuilder.Services;

    /// <inheritdoc />
    public IMessageBusHostBuilder HostBuilder => hostBuilder;

    /// <inheritdoc />
    public Type ContextType => typeof(TContext);

    /// <inheritdoc />
    public string Name { get; } = typeof(TContext).FullName ?? typeof(TContext).Name;
}
