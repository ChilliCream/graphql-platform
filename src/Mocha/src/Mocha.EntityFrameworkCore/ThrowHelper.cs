using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class ThrowHelper
{
    public static InvalidOperationException DbContextNotRegistered(Type contextType)
        => new(
            $"The {nameof(DbContext)} '{contextType.FullName}' is not registered as a service. "
            + $"Register it with {nameof(EntityFrameworkServiceCollectionExtensions.AddDbContext)}, "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextPool)}, "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddDbContextFactory)} or "
            + $"{nameof(EntityFrameworkServiceCollectionExtensions.AddPooledDbContextFactory)}. "
            + $"Before EF Core 10, {nameof(EntityFrameworkServiceCollectionExtensions.AddPooledDbContextFactory)} "
            + "does not register the context itself; also add "
            + $"services.{nameof(ServiceCollectionServiceExtensions.AddScoped)}(sp => "
            + $"sp.{nameof(ServiceProviderServiceExtensions.GetRequiredService)}"
            + $"<{nameof(IDbContextFactory<DbContext>)}<{contextType.Name}>>()"
            + $".{nameof(IDbContextFactory<DbContext>.CreateDbContext)}()).");
}
