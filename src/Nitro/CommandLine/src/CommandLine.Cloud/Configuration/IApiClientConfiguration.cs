namespace ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;

internal interface IApiClientConfiguration
{
    Action<HttpClient>? ConfigureClient { get; }
    Action<IHttpClientBuilder>? ConfigureBuilder { get; }
}
