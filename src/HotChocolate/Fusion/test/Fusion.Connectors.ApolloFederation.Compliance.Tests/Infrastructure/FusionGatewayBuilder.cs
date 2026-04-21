using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.ApolloFederation;
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
    private const string DefaultBaseAddress = "http://localhost/graphql";

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
    public static async Task<FusionGateway> ComposeAsync(
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

                sourceSchemaTexts.Add(new SourceSchemaText(name, info.CompositeSdl));
                subgraphInfos.Add(info);
            }

            var schemaDocument = ComposeSchema(sourceSchemaTexts);
            var settings = BuildGatewaySettings(subgraphInfos);

            var gatewayServices = new ServiceCollection();
            gatewayServices.AddSingleton<IHttpClientFactory>(
                new TestSubgraphHttpClientFactory(hosts));

            gatewayServices
                .AddGraphQLGateway()
                .AddApolloFederationSupport()
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
        var federationSdl = await FetchSubgraphSdlAsync(host).ConfigureAwait(false);
        var transformResult = FederationSchemaTransformer.Transform(federationSdl);

        if (!transformResult.IsSuccess)
        {
            var messages = string.Join(
                ", ",
                transformResult.Errors.Select(static e => e.Message));
            throw new XunitException(
                $"Apollo Federation transform failed for subgraph '{host.Name}': {messages}");
        }

        var compositeSdl = transformResult.Value;
        var lookups = ExtractLookups(compositeSdl);

        return new SubgraphInfo(
            host.Name,
            compositeSdl,
            lookups,
            new Uri(DefaultBaseAddress));
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

    private static DocumentNode ComposeSchema(IReadOnlyList<SourceSchemaText> sourceSchemas)
    {
        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions(),
            compositionLog);

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

        return result.Value.ToSyntaxNode();
    }

    private static JsonDocumentOwner BuildGatewaySettings(IReadOnlyList<SubgraphInfo> subgraphs)
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

                writer.WriteStartObject("extensions");
                writer.WriteStartObject("apolloFederation");
                writer.WriteStartObject("lookups");

                foreach (var (lookupName, info) in subgraph.Lookups)
                {
                    writer.WriteStartObject(lookupName);
                    writer.WriteString("entityType", info.EntityTypeName);

                    if (info.ArgumentToKeyFieldMap.Count > 0)
                    {
                        writer.WriteStartObject("arguments");

                        foreach (var (argument, keyField) in info.ArgumentToKeyFieldMap)
                        {
                            writer.WriteString(argument, keyField);
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
        }

        var document = JsonDocument.Parse(buffer.WrittenMemory);
        return new JsonDocumentOwner(document, EmptyMemoryOwner.Instance);
    }

    private static IReadOnlyDictionary<string, LookupFieldSettings> ExtractLookups(string compositeSdl)
    {
        var document = Utf8GraphQLParser.Parse(compositeSdl);
        var queryName = FindRootQueryName(document) ?? "Query";

        var lookups = new Dictionary<string, LookupFieldSettings>(StringComparer.Ordinal);

        foreach (var definition in document.Definitions)
        {
            if (definition is not ObjectTypeDefinitionNode objectType
                || !string.Equals(objectType.Name.Value, queryName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var field in objectType.Fields)
            {
                if (!HasDirective(field.Directives, "lookup"))
                {
                    continue;
                }

                var entityTypeName = GetNamedTypeName(field.Type);

                if (entityTypeName is null)
                {
                    continue;
                }

                var arguments = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var argument in field.Arguments)
                {
                    var keyFieldName = GetIsFieldName(argument.Directives) ?? argument.Name.Value;
                    arguments[argument.Name.Value] = keyFieldName;
                }

                lookups[field.Name.Value] = new LookupFieldSettings(entityTypeName, arguments);
            }
        }

        return lookups;
    }

    private static string? FindRootQueryName(DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is SchemaDefinitionNode schema)
            {
                foreach (var operationType in schema.OperationTypes)
                {
                    if (operationType.Operation is OperationType.Query)
                    {
                        return operationType.Type.Name.Value;
                    }
                }
            }
        }

        return null;
    }

    private static bool HasDirective(
        IReadOnlyList<DirectiveNode> directives,
        string name)
    {
        foreach (var directive in directives)
        {
            if (string.Equals(directive.Name.Value, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetIsFieldName(IReadOnlyList<DirectiveNode> directives)
    {
        foreach (var directive in directives)
        {
            if (!string.Equals(directive.Name.Value, "is", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var argument in directive.Arguments)
            {
                if (string.Equals(argument.Name.Value, "field", StringComparison.Ordinal)
                    && argument.Value is StringValueNode stringValue)
                {
                    return stringValue.Value;
                }
            }
        }

        return null;
    }

    private static string? GetNamedTypeName(ITypeNode type) => type switch
    {
        NonNullTypeNode nonNull => GetNamedTypeName(nonNull.Type),
        ListTypeNode list => GetNamedTypeName(list.Type),
        NamedTypeNode named => named.Name.Value,
        _ => null
    };

    private sealed record LookupFieldSettings(
        string EntityTypeName,
        IReadOnlyDictionary<string, string> ArgumentToKeyFieldMap);

    private sealed record SubgraphInfo(
        string Name,
        string CompositeSdl,
        IReadOnlyDictionary<string, LookupFieldSettings> Lookups,
        Uri BaseAddress);

    private sealed class TestSubgraphHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, SubgraphHost> _subgraphs;

        public TestSubgraphHttpClientFactory(IReadOnlyList<SubgraphHost> subgraphs)
        {
            _subgraphs = subgraphs.ToDictionary(static s => s.Name, StringComparer.Ordinal);
        }

        public HttpClient CreateClient(string name)
        {
            if (!_subgraphs.TryGetValue(name, out var subgraph))
            {
                throw new InvalidOperationException(
                    $"No subgraph host registered for Apollo Federation subgraph '{name}'.");
            }

            return subgraph.CreateClient();
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
