using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Configuration.Parsers;

/// <summary>
/// Built-in fallback parser that produces default client configurations from a source schema's
/// <c>transports</c> block.
/// </summary>
internal sealed class DefaultGraphQLClientConfigurationParser : ISourceSchemaClientConfigurationParser
{
    public bool TryParse(
        FusionSchemaDefinition schema,
        JsonProperty sourceSchema,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration[]? configurations)
    {
        if (!sourceSchema.Value.TryGetProperty("transports", out var transports)
            || transports.ValueKind != JsonValueKind.Object)
        {
            configurations = null;
            return false;
        }

        var hasHttp = transports.TryGetProperty("http", out var http)
            && http.ValueKind == JsonValueKind.Object;
        var hasWebSockets = transports.TryGetProperty("websockets", out var webSockets)
            && webSockets.ValueKind == JsonValueKind.Object;

        if (!hasHttp && !hasWebSockets)
        {
            configurations = null;
            return false;
        }

        var parsedConfigurations = new List<ISourceSchemaClientConfiguration>(2);

        if (hasHttp)
        {
            parsedConfigurations.Add(
                CreateHttpClientConfiguration(schema, sourceSchema.Name, http, hasWebSockets));
        }

        if (hasWebSockets)
        {
            parsedConfigurations.Add(
                CreateWebSocketClientConfiguration(schema, sourceSchema.Name, webSockets, hasHttp));
        }

        configurations = [.. parsedConfigurations];
        return true;
    }

    private static HttpSourceSchemaClientConfiguration CreateHttpClientConfiguration(
        FusionSchemaDefinition schema,
        string schemaName,
        JsonElement http,
        bool hasWebSockets)
    {
        var clientName = HttpSourceSchemaClientConfiguration.DefaultClientName;

        // An Apollo Federation subgraph is only assumed to speak plain GraphQL, so alias batching
        // is the only batching capability that is defaulted on for it. Every other source schema
        // keeps the protocol-extension defaults. Explicitly declared settings win, per flag.
        var capabilities =
            schema.GetSourceSchemaConnectorKind(schemaName) == ConnectorKindNames.ApolloFederation
                ? SourceSchemaClientCapabilities.AliasBatching
                : SourceSchemaClientCapabilities.Default;
        var supportedOperations = hasWebSockets
            ? SupportedOperationType.Query | SupportedOperationType.Mutation
            : SupportedOperationType.All;
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null;
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null;
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null;
        ErrorHandlingMode? onError = null;

        if (http.TryGetProperty("clientName", out var clientNameProperty)
            && clientNameProperty.ValueKind is JsonValueKind.String
            && clientNameProperty.GetString() is { } customClientName
            && !string.IsNullOrEmpty(customClientName))
        {
            clientName = customClientName;
        }

        if (http.TryGetProperty("capabilities", out var capabilitiesElement))
        {
            if (capabilitiesElement.TryGetProperty("standard", out var standard))
            {
                if (standard.TryGetProperty("formats", out var standardFormats))
                {
                    var builder = ImmutableArray.CreateBuilder<MediaTypeWithQualityHeaderValue>();

                    foreach (var format in standardFormats.EnumerateArray())
                    {
                        builder.Add(MediaTypeWithQualityHeaderValue.Parse(format.GetString()!));
                    }

                    defaultAcceptHeaderValues = builder.ToImmutable();
                }
            }

            capabilities = ParseBatchingCapabilities(capabilitiesElement, capabilities);

            if (capabilitiesElement.TryGetProperty("batching", out var batchingElement))
            {
                if (batchingElement.TryGetProperty("formats", out var batchingFormats))
                {
                    var builder = ImmutableArray.CreateBuilder<MediaTypeWithQualityHeaderValue>();

                    foreach (var format in batchingFormats.EnumerateArray())
                    {
                        builder.Add(MediaTypeWithQualityHeaderValue.Parse(format.GetString()!));
                    }

                    batchingAcceptHeaderValues = builder.ToImmutable();
                }
            }

            if (capabilitiesElement.TryGetProperty("onError", out var onErrorElement)
                && onErrorElement.ValueKind is JsonValueKind.String
                && onErrorElement.GetString() is { } onErrorValue
                && Enum.TryParse<ErrorHandlingMode>(onErrorValue, ignoreCase: true, out var parsedOnError))
            {
                onError = parsedOnError;
            }

            supportedOperations = ParseSupportedOperations(capabilitiesElement, supportedOperations);

            if (capabilitiesElement.TryGetProperty("subscriptions", out var subscriptionsElement)
                && subscriptionsElement.TryGetProperty("formats", out var subscriptionFormats))
            {
                var builder = ImmutableArray.CreateBuilder<MediaTypeWithQualityHeaderValue>();

                foreach (var format in subscriptionFormats.EnumerateArray())
                {
                    builder.Add(MediaTypeWithQualityHeaderValue.Parse(format.GetString()!));
                }

                subscriptionAcceptHeaderValues = builder.ToImmutable();
            }
        }

        supportedOperations = ParseSupportedOperations(http, supportedOperations);

        return new HttpSourceSchemaClientConfiguration(
            name: schemaName,
            httpClientName: clientName,
            baseAddress: new Uri(http.GetProperty("url").GetString()!),
            supportedOperations: supportedOperations,
            capabilities: capabilities,
            onError: onError,
            defaultAcceptHeaderValues: defaultAcceptHeaderValues,
            batchingAcceptHeaderValues: batchingAcceptHeaderValues,
            subscriptionAcceptHeaderValues: subscriptionAcceptHeaderValues);
    }

    private static WebSocketSourceSchemaClientConfiguration CreateWebSocketClientConfiguration(
        FusionSchemaDefinition schema,
        string schemaName,
        JsonElement webSockets,
        bool hasHttp)
    {
        var capabilities =
            schema.GetSourceSchemaConnectorKind(schemaName) == ConnectorKindNames.ApolloFederation
                ? SourceSchemaClientCapabilities.AliasBatching
                : SourceSchemaClientCapabilities.Default;
        var supportedOperations = hasHttp
            ? SupportedOperationType.Subscription
            : SupportedOperationType.All;

        if (webSockets.TryGetProperty("capabilities", out var capabilitiesElement))
        {
            capabilities = ParseBatchingCapabilities(capabilitiesElement, capabilities);
            supportedOperations = ParseSupportedOperations(capabilitiesElement, supportedOperations);
        }

        supportedOperations = ParseSupportedOperations(webSockets, supportedOperations);

        return new WebSocketSourceSchemaClientConfiguration(
            name: schemaName,
            url: new Uri(webSockets.GetProperty("url").GetString()!),
            supportedOperations: supportedOperations,
            capabilities: capabilities);
    }

    private static SourceSchemaClientCapabilities ParseBatchingCapabilities(
        JsonElement capabilitiesElement,
        SourceSchemaClientCapabilities capabilities)
    {
        if (!capabilitiesElement.TryGetProperty("batching", out var batchingElement))
        {
            return capabilities;
        }

        capabilities = ApplyBatchingCapability(
            batchingElement,
            "variableBatching",
            SourceSchemaClientCapabilities.VariableBatching,
            capabilities);
        capabilities = ApplyBatchingCapability(
            batchingElement,
            "requestBatching",
            SourceSchemaClientCapabilities.RequestBatching,
            capabilities);
        return ApplyBatchingCapability(
            batchingElement,
            "aliasBatching",
            SourceSchemaClientCapabilities.AliasBatching,
            capabilities);
    }

    private static SourceSchemaClientCapabilities ApplyBatchingCapability(
        JsonElement batchingElement,
        string name,
        SourceSchemaClientCapabilities capability,
        SourceSchemaClientCapabilities capabilities)
    {
        if (!batchingElement.TryGetProperty(name, out var supported)
            || supported.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return capabilities;
        }

        return supported.GetBoolean() ? capabilities | capability : capabilities & ~capability;
    }

    private static SupportedOperationType ParseSupportedOperations(
        JsonElement transport,
        SupportedOperationType supportedOperations)
    {
        supportedOperations = ApplySupportedOperation(
            transport,
            "query",
            SupportedOperationType.Query,
            supportedOperations);
        supportedOperations = ApplySupportedOperation(
            transport,
            "mutation",
            SupportedOperationType.Mutation,
            supportedOperations);
        return ApplySupportedOperation(
            transport,
            "subscriptions",
            SupportedOperationType.Subscription,
            supportedOperations);
    }

    private static SupportedOperationType ApplySupportedOperation(
        JsonElement transport,
        string name,
        SupportedOperationType operation,
        SupportedOperationType supportedOperations)
    {
        if (!transport.TryGetProperty(name, out var operationElement)
            || !operationElement.TryGetProperty("supported", out var supported)
            || supported.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return supportedOperations;
        }

        return supported.GetBoolean()
            ? supportedOperations | operation
            : supportedOperations & ~operation;
    }
}
