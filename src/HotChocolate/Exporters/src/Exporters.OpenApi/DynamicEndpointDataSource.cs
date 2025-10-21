using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.Exporters.OpenApi;

// TODO: Add an abstraction that can be added to schema services
internal sealed class DynamicEndpointDataSource : EndpointDataSource, IDisposable
{
    private List<Endpoint> _endpoints = [];
    private CancellationTokenSource _cts = new();
    private CancellationChangeToken _changeToken;

    public DynamicEndpointDataSource()
    {
        _changeToken = new CancellationChangeToken(_cts.Token);
    }

    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    public override IChangeToken GetChangeToken() => _changeToken;

    // TODO: Synchronization
    public void SetEndpoints(IEnumerable<ExecutableOpenApiDocument> documents)
    {
        var newEndpoints = new List<Endpoint>();

        foreach (var document in documents)
        {
            var endpoint = CreateEndpoint(document);

            newEndpoints.Add(endpoint);
        }

        _endpoints = newEndpoints;

        NotifyChanged();
    }

    private static Endpoint CreateEndpoint(ExecutableOpenApiDocument document)
    {
        var builder = new RouteEndpointBuilder(
            // TODO: Use proper schema name
            requestDelegate: CreateRequestDelegate(ISchemaDefinition.DefaultName, document),
            routePattern: document.Route,
            // TODO: What does this control?
            order: 0)
        {
            DisplayName = document.HttpMethod + " " + document.Route.RawText
        };

        builder.Metadata.Add(new HttpMethodMetadata([document.HttpMethod]));

        return builder.Build();
    }

    private static RequestDelegate CreateRequestDelegate(string schemaName, ExecutableOpenApiDocument document)
    {
        return async context =>
        {
            var cancellationToken = context.RequestAborted;

            // TODO: Use proxy here
            var provider = context.RequestServices.GetRequiredService<IRequestExecutorProvider>();
            var executor = await provider.GetExecutorAsync(schemaName, cancellationToken).ConfigureAwait(false);

            // TODO: Map to variables
            var routeData = context.GetRouteData();

            var request = OperationRequestBuilder.New()
                .SetDocument(document.Document)
                .SetVariableValues(routeData.Values)
                .Build();

            try
            {
                var result = await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    // TODO: Handle properly
                    return;
                }

                if (result is not IOperationResult operationResult)
                {
                    await Results.StatusCode(500).ExecuteAsync(context);
                    return;
                }

                var jsonWriterOptions = new JsonWriterOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var jsonSerializerOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                // TODO: Extract first field and check for null
                var data = operationResult.Data;

                var bodyWriter = context.Response.BodyWriter;
                // TODO: Cache the writer
                var jsonWriter = new Utf8JsonWriter(bodyWriter, jsonWriterOptions);

                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                JsonValueFormatter.WriteValue(jsonWriter, data, jsonSerializerOptions, JsonNullIgnoreCondition.None);

                await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await Results.StatusCode(500).ExecuteAsync(context);
            }
        };
    }

    private void NotifyChanged()
    {
        var oldCts = _cts;
        _cts = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cts.Token);
        oldCts.Cancel();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
