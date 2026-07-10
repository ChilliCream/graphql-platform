using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace HotChocolate.Fusion;

/// <summary>
/// Builds a Fusion gateway over a set of in-process Apollo Federation subgraphs.
/// Each subgraph factory produces a <see cref="SubgraphHost"/> whose
/// <see cref="Microsoft.AspNetCore.TestHost.TestServer"/> serves the subgraph's
/// <c>/graphql</c> endpoint. The gateway's <see cref="IHttpClientFactory"/> routes
/// source-schema requests through those test servers, so every gateway call flows
/// through the real ASP.NET Core / HotChocolate HTTP pipeline.
/// </summary>
internal static class FusionGatewayBuilder
{
    private static Uri SubgraphAddress(string name) => new($"http://{name}/graphql");

    /// <summary>
    /// Composes a Fusion gateway around the supplied Apollo Federation subgraphs.
    /// </summary>
    /// <param name="subgraphs">
    /// Named subgraph factories. Each factory returns a <see cref="SubgraphHost"/>
    /// whose HotChocolate <c>AddApolloFederation()</c> schema is hosted under
    /// <see cref="Microsoft.AspNetCore.TestHost.TestServer"/>.
    /// </param>
    /// <returns>
    /// The composed <see cref="FusionGateway"/>; dispose it to tear down the
    /// subgraph hosts and the gateway service provider.
    /// </returns>
    public static Task<FusionGateway> ComposeAsync(
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => ComposeAsync(
            capture: null,
            sourceSchemaSettings: null,
            NodeResolution.Gateway,
            allowNonResolvableInterfaceObjects: false,
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            subgraphs);

    public static Task<FusionGateway> ComposeAsync(
        ApolloFederationCompatibilityOptions compatibility,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
    {
        ArgumentNullException.ThrowIfNull(compatibility);

        return ComposeAsync(
            capture: null,
            sourceSchemaSettings: null,
            NodeResolution.Gateway,
            compatibility.AllowNonResolvableInterfaceObjects,
            compatibility.ShareableFieldRuntimeTypeRouting,
            subgraphs);
    }

    /// <summary>
    /// Composes a Fusion gateway around the supplied Apollo Federation subgraphs.
    /// </summary>
    /// <param name="nodeResolution">
    /// Determines how the gateway resolves the <c>Query.node</c> field.
    /// </param>
    /// <param name="subgraphs">Named subgraph factories.</param>
    public static Task<FusionGateway> ComposeAsync(
        NodeResolution nodeResolution,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => ComposeAsync(
            capture: null,
            sourceSchemaSettings: null,
            nodeResolution,
            allowNonResolvableInterfaceObjects: false,
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            subgraphs);

    /// <summary>
    /// Composes a Fusion gateway around the supplied Apollo Federation subgraphs,
    /// optionally recording the outgoing subgraph HTTP requests.
    /// </summary>
    /// <param name="capture">
    /// When not <see langword="null"/>, records every gateway to subgraph HTTP request
    /// so a test can assert the number and shape of the requests that were sent.
    /// </param>
    /// <param name="subgraphs">Named subgraph factories.</param>
    public static Task<FusionGateway> ComposeAsync(
        SubgraphRequestCapture? capture,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => ComposeAsync(
            capture,
            sourceSchemaSettings: null,
            NodeResolution.Gateway,
            allowNonResolvableInterfaceObjects: false,
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            subgraphs);

    /// <summary>
    /// Composes a Fusion gateway around the supplied Apollo Federation subgraphs,
    /// optionally recording the outgoing subgraph HTTP requests and merging
    /// operator-supplied settings into the generated gateway settings document.
    /// </summary>
    /// <param name="capture">
    /// When not <see langword="null"/>, records every gateway to subgraph HTTP request
    /// so a test can assert the number and shape of the requests that were sent.
    /// </param>
    /// <param name="sourceSchemaSettings">
    /// When not <see langword="null"/>, maps a source schema name to a raw JSON object
    /// that is deep-merged into that source schema's settings node, so a test can
    /// declare transport capabilities the way an operator would in gateway settings.
    /// </param>
    /// <param name="subgraphs">Named subgraph factories.</param>
    public static Task<FusionGateway> ComposeAsync(
        SubgraphRequestCapture? capture,
        IReadOnlyDictionary<string, string>? sourceSchemaSettings,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
        => ComposeAsync(
            capture,
            sourceSchemaSettings,
            NodeResolution.Gateway,
            allowNonResolvableInterfaceObjects: false,
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            subgraphs);

    /// <summary>
    /// Composes a Fusion gateway around the supplied Apollo Federation subgraphs,
    /// optionally recording the outgoing subgraph HTTP requests and merging
    /// operator-supplied settings into the generated gateway settings document.
    /// </summary>
    /// <param name="capture">
    /// When not <see langword="null"/>, records every gateway to subgraph HTTP request
    /// so a test can assert the number and shape of the requests that were sent.
    /// </param>
    /// <param name="sourceSchemaSettings">
    /// When not <see langword="null"/>, maps a source schema name to a raw JSON object
    /// that is deep-merged into that source schema's settings node, so a test can
    /// declare transport capabilities the way an operator would in gateway settings.
    /// </param>
    /// <param name="nodeResolution">
    /// Determines how the gateway resolves the <c>Query.node</c> field.
    /// </param>
    /// <param name="subgraphs">Named subgraph factories.</param>
    public static async Task<FusionGateway> ComposeAsync(
        SubgraphRequestCapture? capture,
        IReadOnlyDictionary<string, string>? sourceSchemaSettings,
        NodeResolution nodeResolution,
        bool allowNonResolvableInterfaceObjects,
        ShareableFieldRuntimeTypeRouting shareableFieldRuntimeTypeRouting,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
    {
        ArgumentNullException.ThrowIfNull(subgraphs);

        if (subgraphs.Length == 0)
        {
            throw new ArgumentException(
                "At least one subgraph must be provided.",
                nameof(subgraphs));
        }

        var hosts = new List<SubgraphHost>(subgraphs.Length);
        var sourceSchemaTexts = new List<SourceSchemaText>(subgraphs.Length);
        var subgraphInfos = new List<SubgraphInfo>(subgraphs.Length);

        try
        {
            foreach (var (name, factory) in subgraphs)
            {
                var host = await factory().ConfigureAwait(false);

                if (!string.Equals(host.Name, name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Subgraph factory for '{name}' produced a host named '{host.Name}'.");
                }

                hosts.Add(host);

                var info = await BuildSubgraphInfoAsync(host).ConfigureAwait(false);

                sourceSchemaTexts.Add(new SourceSchemaText(name, info.SourceSchemaSdl));
                subgraphInfos.Add(info);
            }

            var schemaDocument = ComposeSchema(
                sourceSchemaTexts,
                nodeResolution != NodeResolution.Gateway,
                nodeResolution,
                allowNonResolvableInterfaceObjects,
                shareableFieldRuntimeTypeRouting);
            var settings = BuildGatewaySettings(subgraphInfos, sourceSchemaSettings);

            var gatewayServices = new ServiceCollection();
            gatewayServices.AddSingleton<IHttpClientFactory>(
                new TestSubgraphHttpClientFactory(hosts, capture));

            gatewayServices
                .AddGraphQLGateway()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .AddInMemoryConfiguration(schemaDocument, settings);

            var services = gatewayServices.BuildServiceProvider();

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync()
                .ConfigureAwait(false);

            return new FusionGateway(executor, services, hosts);
        }
        catch
        {
            foreach (var host in hosts)
            {
                await host.DisposeAsync().ConfigureAwait(false);
            }

            throw;
        }
    }

    private static async Task<SubgraphInfo> BuildSubgraphInfoAsync(SubgraphHost host)
    {
        // The raw Apollo Federation SDL is composed directly: the composer's source-schema
        // preprocessor detects the federation '@link', applies the federation transforms, and
        // records the connector kind on the schema's feature collection in-process. Because the
        // connector kind is carried as a (non-serialized) feature, the source schema must reach
        // the composer as the original federation SDL rather than a pre-transformed document.
        var federationSdl = await FetchSubgraphSdlAsync(host).ConfigureAwait(false);

        return new SubgraphInfo(
            host.Name,
            federationSdl,
            SubgraphAddress(host.Name));
    }

    private static async Task<string> FetchSubgraphSdlAsync(SubgraphHost host)
    {
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = new StringContent(
                "{\"query\":\"{ _service { sdl } }\"}",
                Encoding.UTF8,
                "application/json")
        };

        using var response = await client.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync()
            .ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

        if (!document.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("_service", out var service)
            || !service.TryGetProperty("sdl", out var sdl)
            || sdl.ValueKind != JsonValueKind.String
            || sdl.GetString() is not { Length: > 0 } sdlText)
        {
            throw new XunitException(
                $"Subgraph '{host.Name}' did not return '_service.sdl' via HTTP.");
        }

        return sdlText;
    }

    private static DocumentNode ComposeSchema(
        IReadOnlyList<SourceSchemaText> sourceSchemas,
        bool enableGlobalObjectIdentification,
        NodeResolution nodeResolution,
        bool allowNonResolvableInterfaceObjects,
        ShareableFieldRuntimeTypeRouting shareableFieldRuntimeTypeRouting)
    {
        var compositionLog = new CompositionLog();
        var options = new SchemaComposerOptions();
        options.ApolloFederationCompatibility.AllowNonResolvableInterfaceObjects =
            allowNonResolvableInterfaceObjects;
        options.ApolloFederationCompatibility.ShareableFieldRuntimeTypeRouting =
            shareableFieldRuntimeTypeRouting;

        // The Apollo Federation transformer already emits every resolvable
        // '@key' as an explicit '@lookup' field with '@is' metadata. Turning
        // off key inference per source schema avoids double-emitting the
        // '@key' directive and prevents the composer from re-introducing
        // list-typed '@key' directives for nested list keys, which the
        // Composite Schema Spec disallows at type level.
        foreach (var sourceSchema in sourceSchemas)
        {
            options.SourceSchemas[sourceSchema.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions
                {
                    InferKeysFromLookups = false
                }
            };
        }

        if (enableGlobalObjectIdentification)
        {
            options.Merger.EnableGlobalObjectIdentification = true;
        }

        options.Merger.NodeResolution = nodeResolution;

        var composer = new SchemaComposer(sourceSchemas, options, compositionLog);

        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            var sb = new StringBuilder();
            sb.Append(result.Errors[0].Message);

            foreach (var entry in compositionLog)
            {
                sb.AppendLine();
                sb.Append(entry.Message);

                if (entry.Extensions is { Count: > 0 } extensions)
                {
                    foreach (var (key, value) in extensions)
                    {
                        sb.AppendLine();
                        sb.Append(" - ");
                        sb.Append(key);
                        sb.Append(": ");

                        if (value is System.Collections.IEnumerable enumerable
                            and not string)
                        {
                            var first = true;
                            foreach (var item in enumerable)
                            {
                                if (!first)
                                {
                                    sb.Append("; ");
                                }
                                sb.Append(item);
                                first = false;
                            }
                        }
                        else
                        {
                            sb.Append(value);
                        }
                    }
                }
            }

            throw new XunitException(sb.ToString());
        }

        return result.Value.ToSyntaxNode();
    }

    private static JsonDocumentOwner BuildGatewaySettings(
        IReadOnlyList<SubgraphInfo> subgraphs,
        IReadOnlyDictionary<string, string>? sourceSchemaSettings)
    {
        var buffer = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteStartObject("sourceSchemas");

            foreach (var subgraph in subgraphs)
            {
                writer.WriteStartObject(subgraph.Name);

                writer.WriteStartObject("transports");
                writer.WriteStartObject("http");
                writer.WriteString("url", subgraph.BaseAddress.ToString());
                writer.WriteEndObject();
                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
        }

        if (sourceSchemaSettings is not { Count: > 0 })
        {
            var document = JsonDocument.Parse(buffer.WrittenMemory);
            return new JsonDocumentOwner(document, EmptyMemoryOwner.Instance);
        }

        var root = JsonNode.Parse(buffer.WrittenMemory.Span)!.AsObject();
        var schemas = root["sourceSchemas"]!.AsObject();

        foreach (var (name, json) in sourceSchemaSettings)
        {
            if (schemas[name] is not JsonObject target)
            {
                throw new InvalidOperationException(
                    $"Settings supplied for unknown source schema '{name}'.");
            }

            DeepMerge(target, JsonNode.Parse(json)!.AsObject());
        }

        var merged = JsonDocument.Parse(root.ToJsonString());
        return new JsonDocumentOwner(merged, EmptyMemoryOwner.Instance);
    }

    // Recursively merges the 'source' JSON object into 'target'. Nested objects are
    // merged key-by-key; every other value (including arrays) replaces the target
    // value, so a test overrides only the keys it declares and leaves the generated
    // url in place.
    private static void DeepMerge(JsonObject target, JsonObject source)
    {
        foreach (var (key, value) in source)
        {
            if (value is JsonObject sourceObject
                && target[key] is JsonObject targetObject)
            {
                DeepMerge(targetObject, sourceObject);
            }
            else
            {
                target[key] = value?.DeepClone();
            }
        }
    }

    private sealed record SubgraphInfo(
        string Name,
        string SourceSchemaSdl,
        Uri BaseAddress);

    // The gateway resolves an HttpClient by the configured client name and then
    // overwrites its BaseAddress from the source-schema configuration, so the
    // client name does not select the endpoint: the request URL does. Every
    // subgraph here is addressed as 'http://{name}/graphql', so requests are
    // dispatched to the matching in-process TestServer by host.
    private sealed class TestSubgraphHttpClientFactory : IHttpClientFactory
    {
        private readonly HostDispatchingHandler _handler;

        public TestSubgraphHttpClientFactory(
            IReadOnlyList<SubgraphHost> subgraphs,
            SubgraphRequestCapture? capture)
        {
            _handler = new HostDispatchingHandler(subgraphs, capture);
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class HostDispatchingHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HttpMessageInvoker> _byHost;
        private readonly SubgraphRequestCapture? _capture;

        public HostDispatchingHandler(
            IReadOnlyList<SubgraphHost> subgraphs,
            SubgraphRequestCapture? capture)
        {
            _byHost = subgraphs.ToDictionary(
                static s => s.Name,
                static s => new HttpMessageInvoker(s.Server.CreateHandler(), disposeHandler: false),
                StringComparer.OrdinalIgnoreCase);
            _capture = capture;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var host = request.RequestUri?.Host;

            if (host is null || !_byHost.TryGetValue(host, out var invoker))
            {
                throw new InvalidOperationException(
                    $"No subgraph host registered for '{host}'.");
            }

            if (_capture is not null && request.Content is { } content)
            {
                // Buffer the body so it can be recorded and still forwarded to the
                // in-process subgraph handler.
                await content.LoadIntoBufferAsync().ConfigureAwait(false);
                var body = await content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _capture.Record(host, body);
            }

            return await invoker.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public static readonly EmptyMemoryOwner Instance = new();

        public Memory<byte> Memory => default;

        public void Dispose()
        {
        }
    }
}
