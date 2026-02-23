#if !NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore.Infrastructure;

/// <summary>
/// Polyfill for <c>IDbContextOptionsConfiguration&lt;TContext&gt;</c> which was introduced in EF Core 9.0.
/// </summary>
/// <typeparam name="TContext">The type of the context these options apply to.</typeparam>
internal interface IDbContextOptionsConfiguration<TContext> where TContext : DbContext
{
    /// <summary>
    /// Applies the specified configuration.
    /// </summary>
    /// <param name="serviceProvider">The service provider available during DbContext configuration.</param>
    /// <param name="optionsBuilder">The options builder to configure.</param>
    void Configure(IServiceProvider serviceProvider, DbContextOptionsBuilder optionsBuilder);
}
#endif
