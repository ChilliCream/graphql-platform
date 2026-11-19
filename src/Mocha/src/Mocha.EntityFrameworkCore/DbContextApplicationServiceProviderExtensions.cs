using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

internal static class DbContextApplicationServiceProviderExtensions
{
    /// <summary>
    /// Gets the application service provider the context options were created from, or <c>null</c> when the
    /// options were not created through dependency injection.
    /// </summary>
    public static IServiceProvider? GetApplicationServiceProvider(this DbContext context)
        => context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
}
