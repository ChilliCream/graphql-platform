using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Holds configuration state for a messaging-enabled DbContext, including service registration
/// delegates that are applied when the DbContext internal service provider is built.
/// </summary>
internal sealed class MessagingDbContextOptions
{
    /// <summary>
    /// Gets the list of delegates that register services into the DbContext internal service provider.
    /// </summary>
    public List<Action<IServiceCollection>> ConfigureServices { get; init; } = [];
}
