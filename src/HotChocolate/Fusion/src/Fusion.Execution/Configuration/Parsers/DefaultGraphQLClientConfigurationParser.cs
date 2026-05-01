using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Configuration.Parsers;

/// <summary>
/// Built-in fallback parser that produces a default
/// <see cref="SourceSchemaHttpClientConfiguration"/> from a source schema's
/// <c>transports.http</c> block.
/// </summary>
internal sealed class DefaultGraphQLClientConfigurationParser : ISourceSchemaClientConfigurationParser
{
    public bool TryParse(
        FusionSchemaDefinition schema,
        JsonProperty sourceSchema,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration[]? configurations)
    {
        if (!sourceSchema.Value.TryGetProperty("transports", out var transports)
            || transports.ValueKind != JsonValueKind.Object
            || !transports.TryGetProperty("http", out var http)
            || http.ValueKind != JsonValueKind.Object)
        {
            configurations = null;
            return false;
        }

        configurations = [CreateHttpClientConfiguration(sourceSchema.Name, http)];
        return true;
    }

    private static SourceSchemaHttpClientConfiguration CreateHttpClientConfiguration(
        string schemaName,
        JsonElement http)
    {
        var clientName = SourceSchemaHttpClientConfiguration.DefaultClientName;
        var capabilities = SourceSchemaClientCapabilities.All;
        var supportedOperations = SupportedOperationType.All;
        ImmutableArray<MediaTypeWithQualityHeaderValue>? defaultAcceptHeaderValues = null;
        ImmutableArray<MediaTypeWithQualityHeaderValue>? batchingAcceptHeaderValues = null;
        ImmutableArray<MediaTypeWithQualityHeaderValue>? subscriptionAcceptHeaderValues = null;

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
                if (standard.TryGetProperty("formats", out var formats))
                {
                    var builder = ImmutableArray.CreateBuilder<MediaTypeWithQualityHeaderValue>();

                    foreach (var format in formats.EnumerateArray())
                    {
                        builder.Add(MediaTypeWithQualityHeaderValue.Parse(format.GetString()!));
                    }

                    defaultAcceptHeaderValues = builder.ToImmutable();
                }
            }

            if (capabilitiesElement.TryGetProperty("batching", out var batchingElement))
            {
                if (batchingElement.TryGetProperty("variableBatching", out var supported)
                    && !supported.GetBoolean())
                {
                    capabilities &= ~SourceSchemaClientCapabilities.VariableBatching;
                }

                if (batchingElement.TryGetProperty("requestBatching", out supported)
                    && !supported.GetBoolean())
                {
                    capabilities &= ~SourceSchemaClientCapabilities.RequestBatching;
                }

                if (batchingElement.TryGetProperty("formats", out var formats))
                {
                    var builder = ImmutableArray.CreateBuilder<MediaTypeWithQualityHeaderValue>();

                    foreach (var format in formats.EnumerateArray())
                    {
                        builder.Add(MediaTypeWithQualityHeaderValue.Parse(format.GetString()!));
                    }

                    batchingAcceptHeaderValues = builder.ToImmutable();
                }
            }

            if (capabilitiesElement.TryGetProperty("subscriptions", out var subscriptionsElement))
            {
                if (subscriptionsElement.TryGetProperty("supported", out var supported)
                    && !supported.GetBoolean())
                {
                    supportedOperations &= ~SupportedOperationType.Subscription;
                }

                if (subscriptionsElement.TryGetProperty("formats", out var formats))
                {
                    var builder = ImmutableArray.CreateBuilder<MediaTypeWithQualityHeaderValue>();

                    foreach (var format in formats.EnumerateArray())
                    {
                        builder.Add(MediaTypeWithQualityHeaderValue.Parse(format.GetString()!));
                    }

                    subscriptionAcceptHeaderValues = builder.ToImmutable();
                }
            }
        }

        return new SourceSchemaHttpClientConfiguration(
            name: schemaName,
            httpClientName: clientName,
            baseAddress: new Uri(http.GetProperty("url").GetString()!),
            supportedOperations: supportedOperations,
            capabilities: capabilities,
            defaultAcceptHeaderValues: defaultAcceptHeaderValues,
            batchingAcceptHeaderValues: batchingAcceptHeaderValues,
            subscriptionAcceptHeaderValues: subscriptionAcceptHeaderValues);
    }
}
