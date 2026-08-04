using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Holds configuration state for a messaging-enabled DbContext, including service registration
/// delegates that are applied when the DbContext internal service provider is built.
/// </summary>
public class MessagingDbContextOptions
{
    /// <summary>
    /// Gets the list of delegates that register services into the DbContext internal service provider.
    /// </summary>
    public List<Action<IServiceProvider, IServiceCollection>> ConfigureServices { get; init; } = [];

    /// <summary>
    /// Gets or sets the application-level service provider used to resolve dependencies during DbContext service configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when accessed before the service provider has been configured.
    /// </exception>
    public IServiceProvider ServiceProvider
    {
        get => field ??
            throw new InvalidOperationException(
                "ServiceProvider has not been configured. Ensure AddEntityFramework<TContext>() has been called on the message bus host builder.");
        set;
    }
}
