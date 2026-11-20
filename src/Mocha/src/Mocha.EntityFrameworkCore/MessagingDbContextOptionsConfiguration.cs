using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Outbox;
using Mocha.Scheduling;

namespace Mocha.EntityFrameworkCore;

internal class MessagingDbContextOptionsConfiguration<TContext>(
    IOptionsMonitor<MessagingDbContextOptions> monitor,
    IServiceProvider applicationServices)
    : IDbContextOptionsConfiguration<TContext> where TContext : DbContext
{
    private readonly MessagingDbContextServices _services = new(
        applicationServices.GetService<IOutboxSignal>(),
        applicationServices.GetService<ISchedulerSignal>(),
        applicationServices.GetTimeProvider());

    /// <summary>
    /// Configures the DbContext options builder by adding the messaging extensions resolved
    /// from the named <see cref="MessagingDbContextOptions"/> for the target context type.
    /// </summary>
    /// <param name="serviceProvider">The service provider available during DbContext configuration.</param>
    /// <param name="optionsBuilder">The options builder to configure with messaging extensions.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="MessagingDbContextOptions"/> are registered for the context type.
    /// </exception>
    public void Configure(IServiceProvider serviceProvider, DbContextOptionsBuilder optionsBuilder)
    {
        var name = typeof(TContext).FullName ?? typeof(TContext).Name;
        var options = monitor.Get(name);
        if (options is null)
        {
            throw new InvalidOperationException($"No options found for context {name}");
        }

        optionsBuilder.AddMessagingExtensions(options, _services);
    }
}
