using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit.Sdk;

namespace HotChocolate.Fusion;

public abstract partial class FusionTestBase : IDisposable
{
    private readonly TestServerSession _testServerSession = new();
    private bool _disposed;

    protected async Task<Gateway> CreateCompositeSchemaAsync(
        (string SchemaName, TestServer Server)[] sourceSchemaServers,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configureApplication = null,
        Action<IFusionGatewayBuilder>? configureGatewayBuilder = null,
        [StringSyntax("json")] string? schemaSettings = null)
    {
        var sourceSchemas = new List<SourceSchemaText>();
        var gatewayServices = new ServiceCollection();
        var gatewayBuilder = gatewayServices.AddGraphQLGatewayServer();
        var interactions = new ConcurrentDictionary<string, ConcurrentDictionary<int, SourceSchemaInteraction>>();

        foreach (var (name, server) in sourceSchemaServers)
        {
            var schemaDocument = await server.Services.GetSchemaAsync(name);
            sourceSchemas.Add(new SourceSchemaText(name, schemaDocument.ToString()));

            var sourceSchemaOptions = server.Services.GetRequiredService<IOptions<SourceSchemaOptions>>().Value;
            gatewayServices.TryAddSingleton<IHttpClientFactory, Factory>();
            gatewayServices.AddSingleton(new TestServerRegistration(name, server, sourceSchemaOptions));

            if (schemaSettings is null)
            {
                gatewayBuilder.AddHttpClientConfiguration(
                    name,
                    new Uri("http://localhost:5000/graphql"),
                    batchingMode: sourceSchemaOptions.BatchingMode,
                    batchingAcceptHeaderValues: sourceSchemaOptions.BatchingAcceptHeaderValues,
                    onBeforeSend: (context, node, request) =>
                    {
                        if (request.Content is not { } content)
                        {
                            throw new InvalidOperationException("Expected content to not be null.");
                        }

                        if (request.Content.Headers.ContentType is not { } contentType)
                        {
                            throw new InvalidOperationException("Expected Content-Type header to not be null.");
                        }

                        var bodyStream = new MemoryStream();
                        var originalStream = content.ReadAsStream();

                        originalStream.CopyTo(bodyStream);
                        bodyStream.Position = 0;

                        if (originalStream.CanSeek)
                        {
                            originalStream.Position = 0;
                        }

                        GetSourceSchemaInteraction(context, node).Request
                            = new SourceSchemaInteraction.RawSourceSchemaRequest
                            {
                                Body = bodyStream,
                                ContentType = contentType
                            };
                    },
                    onAfterReceive: (context, node, response) =>
                    {
                        var interaction = GetSourceSchemaInteraction(context, node);

                        interaction.StatusCode = response.StatusCode;
                        interaction.ContentType = response.Content.Headers.ContentType?.ToString();
                    },
                    onSourceSchemaResult: (context, node, result) =>
                    {
                        var interaction = GetSourceSchemaInteraction(context, node);

                        interaction.Results.Add(SerializeSourceSchemaResult(result));
                    });
            }
        }

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions
        {
            Merger =
            {
                EnableGlobalObjectIdentification = true
            }
        };
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            var sb = new StringBuilder();
            sb.Append(result.Errors[0].Message);

            foreach (var entry in compositionLog)
            {
                sb.AppendLine();
                sb.Append(entry.Message);
            }

            throw new XunitException(sb.ToString());
        }

        JsonDocumentOwner? settings = null;
        if (schemaSettings is not null)
        {
            var body = JsonDocument.Parse(schemaSettings);
            settings = new JsonDocumentOwner(body, new EmptyMemoryOwner());
        }

        gatewayBuilder.AddInMemoryConfiguration(result.Value.ToSyntaxNode(), settings);
        gatewayBuilder.AddHttpRequestInterceptor<OperationPlanHttpRequestInterceptor>();
        gatewayBuilder.ModifyRequestOptions(o =>
        {
            o.CollectOperationPlanTelemetry = false;
            o.AllowErrorHandlingModeOverride = true;
        });
        configureGatewayBuilder?.Invoke(gatewayBuilder);

        configureApplication ??=
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL());
            };

        var gatewayTestServer = _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();

                foreach (var serviceDescriptor in gatewayServices)
                {
                    services.Add(serviceDescriptor);
                }

                configureServices?.Invoke(services);
            },
            configureApplication);

        return new Gateway(gatewayTestServer, sourceSchemas, interactions);

        SourceSchemaInteraction GetSourceSchemaInteraction(OperationPlanContext context, ExecutionNode node)
        {
            var schemaName = node is OperationExecutionNode { SchemaName: { } staticSchemaName }
                ? staticSchemaName
                : context.GetDynamicSchemaName(node);

            var schemaInteractions = interactions.GetOrAdd(schemaName, _ => []);
            return schemaInteractions.GetOrAdd(node.Id, _ => new SourceSchemaInteraction());
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _testServerSession.Dispose();
        }
    }

    protected class Gateway(
        TestServer testServer,
        List<SourceSchemaText> sourceSchemas,
        ConcurrentDictionary<string, ConcurrentDictionary<int, SourceSchemaInteraction>> interactions) : IDisposable
    {
        public HttpClient CreateClient() => testServer.CreateClient();

        public IServiceProvider Services => testServer.Services;

        public List<SourceSchemaText> SourceSchemas => sourceSchemas;

        public ConcurrentDictionary<string, ConcurrentDictionary<int, SourceSchemaInteraction>> Interactions =>
            interactions;

        public void Dispose()
        {
            testServer.Dispose();
        }
    }

    protected class SourceSchemaInteraction
    {
        public RawSourceSchemaRequest? Request { get; set; }

        public List<string> Results { get; } = [];

        public HttpStatusCode? StatusCode { get; set; }

        public string? ContentType { get; set; }

        public sealed class RawSourceSchemaRequest
        {
            public required MemoryStream Body { get; init; }
            public required MediaTypeHeaderValue ContentType { get; init; }
        }
    }

    private sealed class SourceSchemaOptions
    {
        public bool IsOffline { get; set; }

        public bool IsTimingOut { get; set; }

        public SourceSchemaHttpClientBatchingMode BatchingMode { get; set; }

        public ImmutableArray<MediaTypeWithQualityHeaderValue>? BatchingAcceptHeaderValues { get; set; }

        public Action<HttpClient>? ConfigureHttpClient { get; set; }

        public HttpClient? HttpClient { get; set; }
    }

    private sealed class OperationPlanHttpRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.TryAddGlobalState(ExecutionContextData.IncludeOperationPlan, true);
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }

    private class Factory : IHttpClientFactory
    {
        private readonly Dictionary<string, TestServerRegistration> _registrations;

        public Factory(IEnumerable<TestServerRegistration> registrations)
        {
            _registrations = registrations.ToDictionary(r => r.Name, r => r);
        }

        public HttpClient CreateClient(string name)
        {
            if (_registrations.TryGetValue(name, out var registration))
            {
                HttpClient client;

                if (registration.Options.IsOffline)
                {
                    client = new HttpClient(new ErrorHandler());
                }
                else if (registration.Options.IsTimingOut)
                {
                    client = new HttpClient(new TimeoutHandler());
                }
                else if (registration.Options.HttpClient is { } httpClient)
                {
                    return httpClient;
                }
                else
                {
                    client = registration.Server.CreateClient();
                }

                registration.Options.ConfigureHttpClient?.Invoke(client);

                client.DefaultRequestHeaders.AddGraphQLPreflight();

                return client;
            }

            throw new InvalidOperationException(
                $"No test server registered with the name: {name}");
        }

        private class ErrorHandler : HttpClientHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }

        private class TimeoutHandler : HttpClientHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }

    private record TestServerRegistration(
        string Name,
        TestServer Server,
        SourceSchemaOptions Options);

    private class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => default;

        public void Dispose() { }
    }
}
