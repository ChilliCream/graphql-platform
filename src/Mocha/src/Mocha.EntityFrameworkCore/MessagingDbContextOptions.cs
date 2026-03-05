using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Holds configuration state for a messaging-enabled DbContext, including service registration
/// delegates that are applied when the DbContext internal service provider is built.
/// </summary>
public class MessagingDbContextOptions
{
    /// <summary>
    /// Gets or sets the list of delegates that register services into the DbContext internal service provider.
    /// </summary>
    public List<Action<IServiceProvider, IServiceCollection>> ConfigureServices { get; set; } = [];

    /// <summary>
    /// Gets or sets the application-level service provider used to resolve dependencies during DbContext service configuration.
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; } = null!;
}
