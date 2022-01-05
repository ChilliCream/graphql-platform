using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        builder.AddAuthorizationHandler<OpaAuthorizationHandler>();
        builder.Services.AddSingleton<IOpaDecision, OpaDecision>();
        builder.Services.AddHttpClient<IOpaService, OpaService>((f, c) =>
        {
            OpaOptions? options = f.GetRequiredService<OpaOptions>();
            c.BaseAddress = options.BaseAddress;
            c.Timeout = options.ConnectionTimeout;
        });
        builder.Services.AddSingleton(f =>
        {
            var options = new OpaOptions();
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
#if NET5_0_OR_GREATER
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
#else
                IgnoreNullValues = true
#endif
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            options.JsonSerializerOptions = jsonOptions;
            configure?.Invoke(f.GetRequiredService<IConfiguration>(), options);
            return options;
        });
        return builder;
    }
}
