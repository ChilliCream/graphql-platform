using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

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
    public static IRequestExecutorBuilder AddOpaAuthorization(
        this IRequestExecutorBuilder builder,
        Action<IConfiguration, OpaOptions>? configure = null)
    {
        builder.AddAuthorizationHandler<OpaAuthorizationHandler>();
        builder.Services.AddSingleton<IOpaQueryRequestFactory, DefaultQueryRequestFactory>();
        builder.Services.AddHttpClient<IOpaService, OpaService>((f, c) =>
        {
            var options = f.GetRequiredService<IOptions<OpaOptions>>();
            c.BaseAddress = options.Value.BaseAddress;
            c.Timeout = options.Value.Timeout;
        });

        builder.Services.AddOptions<OpaOptions>().Configure<IServiceProvider>((o, f) =>
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            jsonOptions.Converters.Add(
                new JsonStringEnumConverter(
                    JsonNamingPolicy.CamelCase,
                    false));
            o.JsonSerializerOptions = jsonOptions;
            configure?.Invoke(f.GetRequiredService<IConfiguration>(), o);
        });

        return builder;
    }

    /// <summary>
    /// Adds result handler to the OPA options.
    /// </summary>
    /// <param name="builder">Instance of <see cref="IRequestExecutorBuilder"/>.</param>
    /// <param name="policyPath">The path to the policy.</param>
    /// <param name="parseResult">The PDP decision result.</param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddOpaResultHandler(
        this IRequestExecutorBuilder builder,
        string policyPath,
        ParseResult parseResult)
    {
        builder.Services
            .AddOptions<OpaOptions>()
            .Configure<IServiceProvider>(
                (o, _) => o.PolicyResultHandlers.Add(policyPath, parseResult));
        return builder;
    }

    /// <summary>
    /// Adds OPA query request extensions handler to the OPA options.
    /// </summary>
    /// <param name="builder">Instance of <see cref="IRequestExecutorBuilder"/>.</param>
    /// <param name="policyPath">The path to the policy.</param>
    /// <param name="opaQueryRequestExtensionsHandler">The handler for the extensions associated with the Policy.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
    /// </returns>
    public static IRequestExecutorBuilder AddOpaQueryRequestExtensionsHandler(
        this IRequestExecutorBuilder builder,
        string policyPath,
        OpaQueryRequestExtensionsHandler opaQueryRequestExtensionsHandler)
    {
        builder.Services
            .AddOptions<OpaOptions>()
            .Configure<IServiceProvider>(
                (o, _) => o.OpaQueryRequestExtensionsHandlers.Add(policyPath, opaQueryRequestExtensionsHandler));
        return builder;
    }
}
