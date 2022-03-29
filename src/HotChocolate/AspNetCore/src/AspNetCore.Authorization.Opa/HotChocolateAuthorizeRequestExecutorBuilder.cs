using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public static IRequestExecutorBuilder AddOpaAuthorizationHandler(
        this IRequestExecutorBuilder builder, Action<IConfiguration, OpaOptions>? configure = null)
    {
        builder.AddAuthorizationHandler<OpaAuthorizationHandler>();
        builder.Services.AddSingleton<IOpaQueryRequestFactory, DefaultQueryRequestFactory>();
        builder.Services.AddSingleton<IOpaDecision, DefaultOpaDecision>();
        builder.Services.AddSingleton<DefaultPolicyResultHandler>();
        builder.Services.AddHttpClient<IOpaService, OpaService>((f, c) =>
        {
            IOptions<OpaOptions>? options = f.GetRequiredService<IOptions<OpaOptions>>();
            c.BaseAddress = options.Value.BaseAddress;
            c.Timeout = options.Value.ConnectionTimeout;
        });

        builder.Services.AddOptions<OpaOptions>().Configure<IServiceProvider>((o, f) =>
        {
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
            o.JsonSerializerOptions = jsonOptions;
            configure?.Invoke(f.GetRequiredService<IConfiguration>(), o);
        });

        return builder;
    }

    public static IRequestExecutorBuilder AddOpaResponseHandler<T>(this IRequestExecutorBuilder builder, string policyPath, Func<IServiceProvider, T?>? factory=null)
        where T : class, IPolicyResultHandler
    {
        if (factory is not null)
        {
            builder.Services.AddSingleton(factory);
        }
        else
        {
            builder.Services.AddSingleton<T>();
        }

        builder.Services.AddOptions<OpaOptions>()
            .Configure<IServiceProvider>((o, f) =>
            {
                o.PolicyResultHandlers.Add(policyPath, f.GetRequiredService<T>());
            });
        return builder;
    }

    public static IRequestExecutorBuilder AddOpaResponseHandlerAsync<T>(this IRequestExecutorBuilder builder,
        string policyPath, Func<PolicyResultContext<T>, Task<ResponseBase>> func)
    {
        return builder.AddOpaResponseHandler(policyPath,
            f => new DelegatePolicyResultHandler<T, ResponseBase>(func, f.GetRequiredService<IOptions<OpaOptions>>()));
    }

    public static IRequestExecutorBuilder AddOpaResponseHandler<T>(this IRequestExecutorBuilder builder,
        string policyPath, Func<PolicyResultContext<T>, ResponseBase> func)
    {
        return builder.AddOpaResponseHandler(policyPath,
            f => new DelegatePolicyResultHandler<T, ResponseBase>(ctx => Task.FromResult(func(ctx)), f.GetRequiredService<IOptions<OpaOptions>>()));
    }
}
