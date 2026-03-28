using System.Net.Http.Headers;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.Client.Environments;
using ChilliCream.Nitro.Client.FusionConfiguration;
using ChilliCream.Nitro.Client.Mcp;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.Client.PersonalAccessTokens;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.Client.Stages;
using ChilliCream.Nitro.Client.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChilliCream.Nitro.Client;

public static class NitroClientServiceCollectionExtensions
{
    private static readonly string s_userAgent = $"Nitro CLI/{Version}";

    public static IServiceCollection AddNitroApisClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IApisClient, ApisClient>();

        return services;
    }

    public static IServiceCollection AddNitroApiKeysClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IApiKeysClient, ApiKeysClient>();

        return services;
    }

    public static IServiceCollection AddNitroClientsClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IClientsClient, ClientsClient>();

        return services;
    }

    public static IServiceCollection AddNitroEnvironmentsClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IEnvironmentsClient, EnvironmentsClient>();

        return services;
    }

    public static IServiceCollection AddNitroMcpClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IMcpClient, McpClient>();

        return services;
    }

    public static IServiceCollection AddNitroMocksClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IMocksClient, MocksClient>();

        return services;
    }

    public static IServiceCollection AddNitroOpenApiClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IOpenApiClient, OpenApiClient>();

        return services;
    }

    public static IServiceCollection AddNitroPersonalAccessTokensClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IPersonalAccessTokensClient, PersonalAccessTokensClient>();

        return services;
    }

    public static IServiceCollection AddNitroSchemasClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<ISchemasClient, SchemasClient>();

        return services;
    }

    public static IServiceCollection AddNitroStagesClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IStagesClient, StagesClient>();

        return services;
    }

    public static IServiceCollection AddNitroWorkspacesClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IWorkspacesClient, WorkspacesClient>();

        return services;
    }

    public static IServiceCollection AddNitroFusionConfigurationClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services, configure);
        services.TryAddSingleton<IFusionConfigurationClient, FusionConfigurationClient>();

        return services;
    }

    internal static IServiceCollection TryAddNitroApiClient(
        this IServiceCollection services,
        Action<NitroApiClientOptions>? configure)
    {
        if (services.Any(t => t.ServiceType == typeof(ApiClientRegistrationMarker)))
        {
            return services;
        }

        var options = new NitroApiClientOptions();
        configure?.Invoke(options);
        options.EnsureValid();

        services.AddSingleton(options);

        if (!services.Any(t => t.ServiceType == typeof(IHttpClientFactory)))
        {
            services.AddHttpClient();
        }

        var clientBuilder = services.AddHttpClient(
            ApiClient.ClientName,
            static (serviceProvider, client) => ConfigureApiHttpClient(serviceProvider, client));

        options.ConfigureHttpClientBuilder?.Invoke(clientBuilder);

        services.TryAddSingleton<IApiClient>(CreateApiClient);
        services.AddSingleton<ApiClientRegistrationMarker>();

        return services;
    }

    private static void ConfigureApiHttpClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<NitroApiClientOptions>();
        var baseAddress = options.ResolveBaseAddress!(serviceProvider);
        var authHeader = options.ResolveAuthHeader!(serviceProvider);

        if (!baseAddress.IsAbsoluteUri)
        {
            throw new InvalidOperationException("The resolved base address must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(authHeader.Name))
        {
            throw new InvalidOperationException("The resolved auth header name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(authHeader.Value))
        {
            throw new InvalidOperationException("The resolved auth header value cannot be empty.");
        }

        client.BaseAddress = baseAddress;

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        client.DefaultRequestHeaders.Remove(authHeader.Name);
        client.DefaultRequestHeaders.Add(authHeader.Name, authHeader.Value);

        client.DefaultRequestHeaders.Remove(NitroHeaders.GraphQLClientVersion);
        client.DefaultRequestHeaders.Add(NitroHeaders.GraphQLClientVersion, Version);

        client.DefaultRequestHeaders.Remove(NitroHeaders.CCCAgent);
        client.DefaultRequestHeaders.Add(NitroHeaders.CCCAgent, s_userAgent);

        client.DefaultRequestHeaders.Remove(NitroHeaders.GraphQLPreflight);
        client.DefaultRequestHeaders.Add(NitroHeaders.GraphQLPreflight, "1");

        client.DefaultRequestVersion = new Version(2, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        options.ConfigureHttpClient?.Invoke(client);
    }

    private static IApiClient CreateApiClient(IServiceProvider serviceProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(serviceProvider.GetRequiredService<IHttpClientFactory>());
        services.AddApiClient();

        return services.BuildServiceProvider().GetRequiredService<IApiClient>();
    }

    private static string Version
    {
        get
        {
            var version = typeof(NitroClientServiceCollectionExtensions).Assembly.GetName().Version!;
            return new Version(version.Major, version.Minor, version.Build).ToString();
        }
    }

    private static class NitroHeaders
    {
        public const string GraphQLClientVersion = "GraphQL-Client-Version";
        public const string CCCAgent = "ccc-agent";
        public const string GraphQLPreflight = "GraphQL-Preflight";
    }

    private sealed class ApiClientRegistrationMarker;
}
