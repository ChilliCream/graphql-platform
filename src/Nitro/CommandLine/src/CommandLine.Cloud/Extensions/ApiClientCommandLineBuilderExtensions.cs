using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Net.Http.Headers;
using ChilliCream.Nitro.CLI.Client;
using Microsoft.Net.Http.Headers;
using static System.UriKind;

namespace ChilliCream.Nitro.CLI.Option.Binders;

internal static class ApiClientCommandLineBuilderExtensions
{
    private static readonly string _userAgent = $"Nitro CLI/{Version}";
    private const string _clientId = "<<NITRO_GRAPHQL_CLIENT_ID>>"; // TODO: inject client id via build

    public static CommandLineBuilder AddApiClient(this CommandLineBuilder builder)
        => builder
            .AddService(ConfigureClientFactory)
            .AddService(ConfigureApiClient);

    private static IHttpClientFactory ConfigureClientFactory(IServiceProvider sp)
    {
        var parseResult = sp.GetRequiredService<ParseResult>();
        var clientConfig = sp.GetService<IApiClientConfiguration>();
        var sessionService = sp.GetRequiredService<ISessionService>();

        var cloudUrlResult = parseResult.FindResultFor(Opt<CloudUrlOption>.Instance);
        var cloudUrl = parseResult.GetValueForOption(Opt<CloudUrlOption>.Instance)!;
        var apiKeyResult = parseResult.FindResultFor(Opt<ApiKeyOption>.Instance);
        var apiKey = parseResult.GetValueForOption(Opt<ApiKeyOption>.Instance)!;

        var serviceCollection = new ServiceCollection();
        var builder = serviceCollection.AddHttpClient(ApiClient.ClientName,
            client =>
            {
                if (sessionService.Session?.ApiUrl is { } apiUrl
                    && cloudUrlResult is not { IsImplicit: false })
                {
                    client.BaseAddress = new Uri($"https://{apiUrl}/graphql");
                }
                else if (!string.IsNullOrWhiteSpace(cloudUrl))
                {
                    if (Uri.TryCreate(cloudUrl, Absolute, out var uri))
                    {
                        client.BaseAddress = uri;
                    }
                    else if (Uri.TryCreate($"https://{cloudUrl}/graphql", Absolute, out uri))
                    {
                        client.BaseAddress = uri;
                    }
                    else
                    {
                        throw new ExitException($"Could not parse cloud URL: {cloudUrl}");
                    }
                }
                else
                {
                    throw new ExitException(
                        $"Could not find any api URL. Either specify --cloud-url or run {"nitro login".AsCommand()}");
                }

                if (sessionService.Session?.Tokens?.AccessToken is { } token
                    && apiKeyResult is not { IsImplicit: false })
                {
                    client.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {token}");
                }
                else if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Add(Headers.ApiKey, apiKey);
                }
                else
                {
                    throw new ExitException(
                        $"You are not authenticated. Either specify --api-key or run {"nitro login".AsCommand()}");
                }

                client.DefaultRequestHeaders.Accept
                    .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Add(Headers.GraphQLClientId, _clientId);
                client.DefaultRequestHeaders.Add(Headers.GraphQLClientVersion, Version);
                client.DefaultRequestHeaders.Add(Headers.CCCAgent, _userAgent);

                client.DefaultRequestHeaders.Add(Headers.GraphQLPreflight, "1");

                client.DefaultRequestVersion = new Version(2, 0);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

                clientConfig?.ConfigureClient?.Invoke(client);
            });
        clientConfig?.ConfigureBuilder?.Invoke(builder);

        return serviceCollection
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
    }

    private static IApiClient ConfigureApiClient(IServiceProvider sp)
    {
        var services = new ServiceCollection();
        services
            .AddSingleton(sp.GetRequiredService<IHttpClientFactory>())
            .AddApiClient();

        return services.BuildServiceProvider().GetRequiredService<IApiClient>();
    }

    static string Version
    {
        get
        {
            var version = typeof(ApiClientCommandLineBuilderExtensions).Assembly.GetName().Version!;
            return new Version(version.Major, version.Minor, version.Build).ToString();
        }
    }
}
