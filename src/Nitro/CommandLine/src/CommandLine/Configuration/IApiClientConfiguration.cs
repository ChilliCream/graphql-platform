namespace ChilliCream.Nitro.CommandLine.Configuration;

internal interface IApiClientConfiguration
{
    Action<HttpClient>? ConfigureClient { get; }
    Action<IHttpClientBuilder>? ConfigureBuilder { get; }
}
