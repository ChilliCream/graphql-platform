using HotChocolate.Execution;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class HttpSourceSchemaClientFactory
    : SourceSchemaClientFactory<HttpSourceSchemaClientConfiguration>
    , IReusableSourceSchemaClientFactory
{
    private static readonly TimeSpan s_maxClientAge = TimeSpan.FromSeconds(30);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<HttpClientFactoryOptions>? _httpClientFactoryOptions;
    private readonly bool _isDefaultHttpClientFactory;

    public HttpSourceSchemaClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<HttpClientFactoryOptions>? httpClientFactoryOptions)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        _httpClientFactory = httpClientFactory;
        _httpClientFactoryOptions = httpClientFactoryOptions;
        _isDefaultHttpClientFactory =
            httpClientFactory.GetType().FullName == "Microsoft.Extensions.Http.DefaultHttpClientFactory";
    }

    protected override ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        HttpSourceSchemaClientConfiguration configuration)
    {
        var httpClient = _httpClientFactory.CreateClient(configuration.HttpClientName);
        httpClient.BaseAddress = configuration.BaseAddress;

        return new HttpSourceSchemaClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: true),
            configuration,
            schema.GetRequestOptions().AnnotateOperationKind);
    }

    public TimeSpan GetMaxClientAge(ISourceSchemaClientConfiguration configuration)
    {
        // Only clients of the default IHttpClientFactory without HttpClient configuration callbacks are reused.
        if (!_isDefaultHttpClientFactory
            || _httpClientFactoryOptions is null
            || configuration is not HttpSourceSchemaClientConfiguration httpConfiguration)
        {
            return TimeSpan.Zero;
        }

        var options = _httpClientFactoryOptions.Get(httpConfiguration.HttpClientName);

        if (options.HttpClientActions.Count > 0)
        {
            return TimeSpan.Zero;
        }

        var handlerLifetime = options.HandlerLifetime;

        // A reused client is replaced after a quarter of the handler lifetime, at most after 30 seconds.
        if (handlerLifetime == Timeout.InfiniteTimeSpan)
        {
            return s_maxClientAge;
        }

        var maxAge = handlerLifetime / 4;
        return maxAge < s_maxClientAge ? maxAge : s_maxClientAge;
    }
}
