using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for the GraphQL builder.
/// </summary>
public static class HotChocolateAuthorizeRequestExecutorBuilder
{
    /// <summary>
    /// Adds OPA authorization handler.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// Configure <see cref="OpaOptions"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddOpaAuthorizationHandler(
        this IRequestExecutorBuilder builder, Action<IConfiguration, OpaOptions>? configure = null)
    {
        builder.AddAuthorization();
        builder.AddAuthorizationHandler<OpaAuthorizationHandler>();
        builder.Services.AddSingleton<IOpaDecision, OpaDecision>();
        builder.Services.AddHttpClient<OpaService>((f, c) =>
        {
            OpaOptions? options = f.GetRequiredService<OpaOptions>();
            c.BaseAddress = options.BaseAddress;
            c.Timeout = options.ConnectionTimeout;
        });
        builder.Services.AddSingleton(f =>
        {
            var options = new OpaOptions();
            configure?.Invoke(f.GetRequiredService<IConfiguration>(), options);
            return options;
        });
        return builder;
    }
}
