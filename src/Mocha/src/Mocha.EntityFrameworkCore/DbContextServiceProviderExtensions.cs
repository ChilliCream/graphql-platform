using Microsoft.EntityFrameworkCore;

namespace Mocha.EntityFrameworkCore;

internal static class DbContextServiceProviderExtensions
{
    /// <summary>
    /// Gets the <see cref="DbContext"/> of the specified type, and throws when it is not registered.
    /// </summary>
    public static DbContext GetRequiredDbContext(this IServiceProvider services, Type contextType)
        => (DbContext?)services.GetService(contextType)
            ?? throw ThrowHelper.DbContextNotRegistered(contextType);
}
