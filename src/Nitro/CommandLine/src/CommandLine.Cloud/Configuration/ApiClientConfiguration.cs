namespace ChilliCream.Nitro.CLI.Option.Binders;

internal sealed class ApiClientConfiguration : IApiClientConfiguration
{
    private ApiClientConfiguration(
        Action<HttpClient>? configureClient,
        Action<IHttpClientBuilder>? configureBuilder)
    {
        ConfigureClient = configureClient;
        ConfigureBuilder = configureBuilder;
    }

    public Action<HttpClient>? ConfigureClient { get; }

    public Action<IHttpClientBuilder>? ConfigureBuilder { get; }

    public static ApiClientConfiguration Create(
        Action<HttpClient>? configureClient = null,
        Action<IHttpClientBuilder>? configureBuilder = null)
    {
        return new ApiClientConfiguration(configureClient, configureBuilder);
    }
}
