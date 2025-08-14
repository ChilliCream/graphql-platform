namespace ChilliCream.Nitro.CLI.Option.Binders;

internal interface IApiClientConfiguration
{
    Action<HttpClient>? ConfigureClient { get; }
    Action<IHttpClientBuilder>? ConfigureBuilder { get; }
}
