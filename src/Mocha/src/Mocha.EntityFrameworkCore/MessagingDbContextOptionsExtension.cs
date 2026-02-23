using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// An EF Core <see cref="IDbContextOptionsExtension"/> that injects messaging services (outbox interceptors,
/// saga support) into the DbContext internal service provider during context construction.
/// </summary>
/// <param name="options">The messaging options containing service configuration delegates to apply.</param>
internal class MessagingDbContextOptionsExtension(MessagingDbContextOptions options) : IDbContextOptionsExtension
{
    /// <summary>
    /// Gets the extension metadata used by EF Core for service provider caching and debug output.
    /// </summary>
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    /// <summary>
    /// Applies all registered messaging service configuration delegates to the DbContext
    /// internal <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The DbContext internal service collection to populate.</param>
    public void ApplyServices(IServiceCollection services)
    {
        foreach (var configureService in options.ConfigureServices)
        {
            configureService(options.ServiceProvider, services);
        }
    }

    /// <summary>
    /// Validates the extension options. No validation is required for messaging extensions.
    /// </summary>
    /// <param name="_">The DbContext options (unused).</param>
    public void Validate(IDbContextOptions _) { }

    /// <summary>
    /// Provides metadata about the messaging extension for EF Core service provider caching and diagnostics.
    /// </summary>
    private class ExtensionInfo : DbContextOptionsExtensionInfo
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtensionInfo"/> for the specified extension.
        /// </summary>
        /// <param name="extension">The parent options extension.</param>
        public ExtensionInfo(IDbContextOptionsExtension extension) : base(extension) { }

        /// <summary>
        /// Returns a hash code used by EF Core to determine whether the internal service provider can be reused.
        /// </summary>
        /// <returns>A constant hash code since messaging services do not vary per provider.</returns>
        public override int GetServiceProviderHashCode()
        {
            return 0;
        }

        /// <summary>
        /// Indicates that contexts with this extension can share the same internal service provider.
        /// </summary>
        /// <param name="other">The other extension info to compare against.</param>
        /// <returns>Always <c>true</c> because messaging extensions are stateless at the provider level.</returns>
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
        {
            return true;
        }

        /// <summary>
        /// Populates debug information for diagnostic purposes, indicating that messaging extensions are active.
        /// </summary>
        /// <param name="debugInfo">The dictionary to populate with debug key-value pairs.</param>
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo.Add("MessagingExtensions", "true");
        }

        /// <summary>
        /// Gets a value indicating this extension is not a database provider.
        /// </summary>
        public override bool IsDatabaseProvider { get; }

        /// <summary>
        /// Gets the log fragment appended to EF Core diagnostic output when this extension is active.
        /// </summary>
        public override string LogFragment { get; } = "MessagingExtensions";
    }
}
