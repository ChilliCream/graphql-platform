using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

internal static class DbContextMessagingServicesExtensions
{
    /// <summary>
    /// Gets the application services carried by the messaging extension of the context options.
    /// The context options must contain the messaging extension.
    /// </summary>
    public static MessagingDbContextServices GetMessagingServices(this DbContext context)
        => context.GetService<IDbContextOptions>().FindExtension<MessagingDbContextOptionsExtension>()!.Services;
}
