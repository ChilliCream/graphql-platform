using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Fusion.Transport.Http;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class HttpSourceSchemaClientFactory
    : SourceSchemaClientFactory<HttpSourceSchemaClientConfiguration>
    , ISharedSourceSchemaClientFactory
{
    private static readonly TimeSpan s_maxClientAge = TimeSpan.FromMinutes(2);
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly ConditionalWeakTable<HttpSourceSchemaClientConfiguration, SharedClient> _sharedClients = [];
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

    public bool TryGetSharedClient(
        FusionSchemaDefinition schema,
        ISourceSchemaClientConfiguration configuration,
        [NotNullWhen(true)] out ISourceSchemaClient? client,
        out long expiresAt)
    {
        // Only clients of the default IHttpClientFactory are shared.
        if (!_isDefaultHttpClientFactory
            || _httpClientFactoryOptions is null
            || configuration is not HttpSourceSchemaClientConfiguration httpConfiguration)
        {
            client = null;
            expiresAt = 0;
            return false;
        }

        var annotateOperationKind = schema.GetRequestOptions().AnnotateOperationKind;
        var now = Stopwatch.GetTimestamp();

        if (!_sharedClients.TryGetValue(httpConfiguration, out var sharedClient)
            || !sharedClient.CanServe(annotateOperationKind, now))
        {
            lock (_sync)
            {
                if (!_sharedClients.TryGetValue(httpConfiguration, out sharedClient)
                    || !sharedClient.CanServe(annotateOperationKind, now))
                {
                    sharedClient = CreateSharedClient(schema, httpConfiguration, annotateOperationKind, now);
                    _sharedClients.AddOrUpdate(httpConfiguration, sharedClient);
                }
            }
        }

        client = sharedClient.Client;
        expiresAt = sharedClient.ExpiresAt;
        return client is not null;
    }

    private SharedClient CreateSharedClient(
        FusionSchemaDefinition schema,
        HttpSourceSchemaClientConfiguration configuration,
        bool annotateOperationKind,
        long now)
    {
        var maxAge = GetMaxClientAge(configuration);

        if (maxAge == TimeSpan.Zero)
        {
            return new SharedClient(null, annotateOperationKind, long.MaxValue);
        }

        // The client this one replaces is never disposed, because requests that took it before it
        // expired may still be sending with it, and disposing it would cancel them. Its handler
        // belongs to the IHttpClientFactory, so the garbage collector reclaims the replaced client.
        var client = CreateClient(schema, configuration);
        var expiresAt = now + (long)(maxAge.TotalSeconds * Stopwatch.Frequency);
        return new SharedClient(client, annotateOperationKind, expiresAt);
    }

    internal TimeSpan GetMaxClientAge(HttpSourceSchemaClientConfiguration configuration)
    {
        var options = _httpClientFactoryOptions!.Get(configuration.HttpClientName);

        // Clients with HttpClient configuration callbacks are created per request.
        if (options.HttpClientActions.Count > 0)
        {
            return TimeSpan.Zero;
        }

        var handlerLifetime = options.HandlerLifetime;

        // A shared client is replaced after the handler lifetime, at most after 2 minutes.
        if (handlerLifetime == Timeout.InfiniteTimeSpan || handlerLifetime > s_maxClientAge)
        {
            return s_maxClientAge;
        }

        return handlerLifetime;
    }

    private sealed record SharedClient(ISourceSchemaClient? Client, bool AnnotateOperationKind, long ExpiresAt)
    {
        public bool CanServe(bool annotateOperationKind, long now)
            => AnnotateOperationKind == annotateOperationKind && now < ExpiresAt;
    }
}
