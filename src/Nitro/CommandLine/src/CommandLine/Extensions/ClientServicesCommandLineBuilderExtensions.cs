using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.ApiKeys;
using ChilliCream.Nitro.CommandLine.Configuration;
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
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static System.UriKind;

namespace ChilliCream.Nitro.CommandLine;

internal static class ClientServicesCommandLineBuilderExtensions
{
    public static CommandLineBuilder AddNitroCloudClients(this CommandLineBuilder builder)
    {
        return builder
            .AddService<ClientServiceProvider>(sp =>
                new ClientServiceProvider(CreateClientServices(sp)))
            .AddService<IApisClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IApisClient>())
            .AddService<IApiKeysClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IApiKeysClient>())
            .AddService<IClientsClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IClientsClient>())
            .AddService<IEnvironmentsClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IEnvironmentsClient>())
            .AddService<IMcpClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IMcpClient>())
            .AddService<IMocksClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IMocksClient>())
            .AddService<IOpenApiClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IOpenApiClient>())
            .AddService<IPersonalAccessTokensClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IPersonalAccessTokensClient>())
            .AddService<ISchemasClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<ISchemasClient>())
            .AddService<IStagesClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IStagesClient>())
            .AddService<IWorkspacesClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IWorkspacesClient>())
            .AddService<IFusionConfigurationClient>(sp =>
                sp.GetRequiredService<ClientServiceProvider>().Services.GetRequiredService<IFusionConfigurationClient>());
    }

    private static IServiceProvider CreateClientServices(IServiceProvider serviceProvider)
    {
        var (baseAddress, authHeader) = ResolveApiClientContext(serviceProvider);

        var services = new ServiceCollection();

        services
            .AddNitroApisClient(options => ConfigureOptions(options, baseAddress, authHeader))
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

        return services.BuildServiceProvider();
    }

    private static void ConfigureOptions(
        NitroApiClientOptions options,
        Uri baseAddress,
        NitroAuthHeader authHeader)
    {
        options.ResolveBaseAddress = _ => baseAddress;
        options.ResolveAuthHeader = _ => authHeader;
        options.ConfigureHttpClient = client =>
        {
            if (!string.IsNullOrWhiteSpace(BuildSecrets.NitroApiClientId))
            {
                client.DefaultRequestHeaders.Remove(Headers.GraphQLClientId);
                client.DefaultRequestHeaders.Add(Headers.GraphQLClientId, BuildSecrets.NitroApiClientId);
            }
        };
    }

    private static (Uri BaseAddress, NitroAuthHeader AuthHeader) ResolveApiClientContext(
        IServiceProvider serviceProvider)
    {
        var parseResult = serviceProvider.GetRequiredService<ParseResult>();
        var sessionService = serviceProvider.GetRequiredService<ISessionService>();

        var cloudUrlResult = parseResult.FindResultFor(Opt<CloudUrlOption>.Instance);
        var cloudUrl = parseResult.GetValueForOption(Opt<CloudUrlOption>.Instance)!;
        var apiKeyResult = parseResult.FindResultFor(Opt<ApiKeyOption>.Instance);
        var apiKey = parseResult.GetValueForOption(Opt<ApiKeyOption>.Instance)!;

        Uri baseAddress;

        if (sessionService.Session?.ApiUrl is { } apiUrl
            && cloudUrlResult is not { IsImplicit: false })
        {
            baseAddress = new Uri($"https://{apiUrl}/graphql");
        }
        else if (!string.IsNullOrWhiteSpace(cloudUrl))
        {
            if (!Uri.TryCreate(cloudUrl, Absolute, out var uri)
                && !Uri.TryCreate($"https://{cloudUrl}", Absolute, out uri))
            {
                throw new ExitException($"Could not parse cloud URL: {cloudUrl}");
            }

            var uriBuilder = new UriBuilder(uri)
            {
                Path = "/graphql",
                Query = string.Empty,
                Fragment = string.Empty,
                UserName = string.Empty,
                Password = string.Empty
            };

            baseAddress = uriBuilder.Uri;
        }
        else
        {
            throw new ExitException(
                $"Could not find any API URL. Either specify --cloud-url or run {"nitro login".AsCommand()}");
        }

        NitroAuthHeader authHeader;

        if (sessionService.Session?.Tokens?.AccessToken is { } token
            && apiKeyResult is not { IsImplicit: false })
        {
            authHeader = new NitroAuthHeader(HeaderNames.Authorization, $"Bearer {token}");
        }
        else if (!string.IsNullOrWhiteSpace(apiKey))
        {
            authHeader = new NitroAuthHeader(Headers.ApiKey, apiKey);
        }
        else
        {
            throw new ExitException(
                $"You are not authenticated. Either specify --api-key or run {"nitro login".AsCommand()}");
        }

        return (baseAddress, authHeader);
    }

    private sealed record ClientServiceProvider(IServiceProvider Services);
}
