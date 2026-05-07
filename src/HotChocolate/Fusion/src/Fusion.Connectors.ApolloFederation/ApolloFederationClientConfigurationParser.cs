using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Connectors.ApolloFederation;

/// <summary>
/// Parses source schema settings for Apollo Federation connectors and produces
/// an <see cref="ApolloFederationSourceSchemaClientConfiguration"/>.
/// </summary>
internal sealed class ApolloFederationClientConfigurationParser : ISourceSchemaClientConfigurationParser
{
    private const string ApolloKind = "Apollo";

    public bool TryParse(
        FusionSchemaDefinition schema,
        JsonProperty sourceSchema,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration[]? configurations)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var connectorKind = schema.GetSourceSchemaConnectorKind(sourceSchema.Name);
        if (!string.Equals(connectorKind, ApolloKind, StringComparison.Ordinal))
        {
            configurations = null;
            return false;
        }

        if (!sourceSchema.Value.TryGetProperty("transports", out var transports)
            || transports.ValueKind != JsonValueKind.Object
            || !transports.TryGetProperty("http", out var http)
            || http.ValueKind != JsonValueKind.Object)
        {
            configurations = null;
            return false;
        }

        configurations = [CreateConfiguration(sourceSchema.Name, http)];
        return true;
    }

    private static ApolloFederationSourceSchemaClientConfiguration CreateConfiguration(
        string schemaName,
        JsonElement http)
    {
        if (!http.TryGetProperty("url", out var urlProperty)
            || urlProperty.ValueKind != JsonValueKind.String
            || urlProperty.GetString() is not { Length: > 0 } url)
        {
            throw new InvalidOperationException(
                $"Source schema '{schemaName}' has no 'transports.http.url' value.");
        }

        var clientName = schemaName;

        if (http.TryGetProperty("clientName", out var clientNameProperty)
            && clientNameProperty.ValueKind == JsonValueKind.String
            && clientNameProperty.GetString() is { Length: > 0 } customClientName)
        {
            clientName = customClientName;
        }

        return new ApolloFederationSourceSchemaClientConfiguration(
            schemaName,
            clientName,
            new Uri(url));
    }
}
