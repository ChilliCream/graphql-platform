using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Provides configuration for the default outbox used by the message bus.
/// </summary>
public static class DefaultOutboxMessageBusHostBuilderExtensions
{
    /// <summary>
    /// Selects the DbContext outbox used by the message bus.
    /// </summary>
    public static IMessageBusHostBuilder UseDefaultOutbox<TContext>(this IMessageBusHostBuilder builder)
        where TContext : DbContext
    {
        builder.Services.Replace(ServiceDescriptor.Singleton(
            new DefaultOutboxDbContextRegistration(typeof(TContext))));
        return builder;
    }
}
