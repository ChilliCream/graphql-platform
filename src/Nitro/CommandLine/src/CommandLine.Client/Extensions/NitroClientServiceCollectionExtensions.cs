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

    public static IServiceCollection AddNitroClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddNitroApisClient()
            .AddNitroApiKeysClient()
            .AddNitroClientsClient()
            .AddNitroEnvironmentsClient()
            .AddNitroMcpClient()
            .AddNitroMocksClient()
            .AddNitroOpenApiClient()
            .AddNitroPersonalAccessTokensClient()
            .AddNitroSchemasClient()
            .AddNitroStagesClient()
            .AddNitroWorkspacesClient()
            .AddNitroFusionConfigurationClient();

        return services;
    }

    public static IServiceCollection AddNitroApisClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IApisClient, ApisClient>();

        return services;
    }

    public static IServiceCollection AddNitroApiKeysClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IApiKeysClient, ApiKeysClient>();

        return services;
    }

    public static IServiceCollection AddNitroClientsClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IClientsClient, ClientsClient>();

        return services;
    }

    public static IServiceCollection AddNitroEnvironmentsClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IEnvironmentsClient, EnvironmentsClient>();

        return services;
    }

    public static IServiceCollection AddNitroMcpClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IMcpClient, McpClient>();

        return services;
    }

    public static IServiceCollection AddNitroMocksClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IMocksClient, MocksClient>();

        return services;
    }

    public static IServiceCollection AddNitroOpenApiClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IOpenApiClient, OpenApiClient>();

        return services;
    }

    public static IServiceCollection AddNitroPersonalAccessTokensClient(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IPersonalAccessTokensClient, PersonalAccessTokensClient>();

        return services;
    }

    public static IServiceCollection AddNitroSchemasClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<ISchemasClient, SchemasClient>();

        return services;
    }

    public static IServiceCollection AddNitroStagesClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IStagesClient, StagesClient>();

        return services;
    }

    public static IServiceCollection AddNitroWorkspacesClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IWorkspacesClient, WorkspacesClient>();

        return services;
    }

    public static IServiceCollection AddNitroFusionConfigurationClient(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        TryAddNitroApiClient(services);
        services.TryAddSingleton<IFusionConfigurationClient, FusionConfigurationClient>();

        return services;
    }

    internal static void TryAddNitroApiClient(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(IApiClient)))
        {
            return;
        }

        services.AddHttpClient(ApiClient.ClientName, static (sp, client) => ConfigureApiHttpClient(sp, client));
        services.TryAddSingleton<IApiClient>(CreateApiClient);
    }

    private static void ConfigureApiHttpClient(IServiceProvider sp, HttpClient client)
    {
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        client.DefaultRequestHeaders.Remove(NitroClientHeaders.GraphQLPreflight);
        client.DefaultRequestHeaders.Add(NitroClientHeaders.GraphQLPreflight, "1");

        client.DefaultRequestHeaders.Remove(NitroClientHeaders.GraphQLClientVersion);
        client.DefaultRequestHeaders.Add(NitroClientHeaders.GraphQLClientVersion, Version);

        client.DefaultRequestHeaders.Remove(NitroClientHeaders.CccAgent);
        client.DefaultRequestHeaders.Add(NitroClientHeaders.CccAgent, s_userAgent);

        client.DefaultRequestVersion = new Version(2, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        var provider = sp.GetRequiredService<INitroClientContextProvider>();
        client.BaseAddress = provider.Url;

        switch (provider.Authorization)
        {
            case NitroClientApiKeyAuthorization apiKey:
                client.DefaultRequestHeaders.Remove(NitroClientHeaders.ApiKey);
                client.DefaultRequestHeaders.Add(NitroClientHeaders.ApiKey, apiKey.ApiKey);
                break;

            case NitroClientAccessTokenAuthorization accessToken:
                client.DefaultRequestHeaders.Remove("Authorization");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.AccessToken}");
                break;

            case null:
                throw new InvalidOperationException(
                    "You are not authenticated. Either specify --api-key or run 'nitro login'.");
        }
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
}
