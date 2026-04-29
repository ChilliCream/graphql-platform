using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Connectors.ApolloFederation;

/// <summary>
/// Parser that claims the <c>http</c> transport for source schemas that carry an
/// <c>extensions.apolloFederation</c> block and produces an
/// <see cref="ApolloFederationSourceSchemaClientConfiguration"/>.
/// </summary>
internal sealed class ApolloFederationClientConfigurationParser : ISourceSchemaClientConfigurationParser
{
    public bool TryParse(
        JsonProperty sourceSchema,
        JsonProperty transport,
        [NotNullWhen(true)] out ISourceSchemaClientConfiguration? configuration)
    {
        if (!sourceSchema.Value.TryGetProperty("extensions", out var extensions)
            || extensions.ValueKind != JsonValueKind.Object
            || !extensions.TryGetProperty("apolloFederation", out var federation)
            || federation.ValueKind != JsonValueKind.Object)
        {
            configuration = null;
            return false;
        }

        if (!transport.Name.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            configuration = null;
            return false;
        }

        configuration = CreateConfiguration(sourceSchema.Name, transport.Value, federation);
        return true;
    }

    private static ApolloFederationSourceSchemaClientConfiguration CreateConfiguration(
        string schemaName,
        JsonElement http,
        JsonElement federation)
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

        var lookups = ParseLookups(schemaName, federation);
        var entityRequires = ParseEntityRequires(schemaName, federation);

        return new ApolloFederationSourceSchemaClientConfiguration(
            schemaName,
            clientName,
            new Uri(url),
            lookups,
            entityRequires);
    }

    private static Dictionary<string, LookupFieldInfo> ParseLookups(
        string schemaName,
        JsonElement federation)
    {
        if (!federation.TryGetProperty("lookups", out var lookupsElement)
            || lookupsElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var lookups = new Dictionary<string, LookupFieldInfo>(StringComparer.Ordinal);

        foreach (var lookup in lookupsElement.EnumerateObject())
        {
            if (lookup.Value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Source schema '{schemaName}' lookup '{lookup.Name}' must be a JSON object.");
            }

            if (!lookup.Value.TryGetProperty("entityType", out var entityTypeProperty)
                || entityTypeProperty.ValueKind != JsonValueKind.String
                || entityTypeProperty.GetString() is not { Length: > 0 } entityType)
            {
                throw new InvalidOperationException(
                    $"Source schema '{schemaName}' lookup '{lookup.Name}' is missing an 'entityType' string.");
            }

            var argumentToKeyFieldMap = ParseArguments(schemaName, lookup.Name, lookup.Value);

            lookups[lookup.Name] = new LookupFieldInfo
            {
                EntityTypeName = entityType,
                ArgumentToKeyFieldMap = argumentToKeyFieldMap
            };
        }

        return lookups;
    }

    private static Dictionary<string, EntityRequiresInfo> ParseEntityRequires(
        string schemaName,
        JsonElement federation)
    {
        if (!federation.TryGetProperty("entityTypes", out var entityTypesElement)
            || entityTypesElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var entityRequires = new Dictionary<string, EntityRequiresInfo>(StringComparer.Ordinal);

        foreach (var entityType in entityTypesElement.EnumerateObject())
        {
            if (entityType.Value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Source schema '{schemaName}' entity type '{entityType.Name}' must be a JSON object.");
            }

            if (!entityType.Value.TryGetProperty("fields", out var fieldsElement)
                || fieldsElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var fields = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);

            foreach (var field in fieldsElement.EnumerateObject())
            {
                if (field.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        $"Source schema '{schemaName}' entity type '{entityType.Name}' "
                        + $"field '{field.Name}' must be a JSON object.");
                }

                if (!field.Value.TryGetProperty("requires", out var requiresElement)
                    || requiresElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var requires = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var argument in requiresElement.EnumerateObject())
                {
                    if (argument.Value.ValueKind != JsonValueKind.String
                        || argument.Value.GetString() is not { Length: > 0 } path)
                    {
                        throw new InvalidOperationException(
                            $"Source schema '{schemaName}' entity type '{entityType.Name}' "
                            + $"field '{field.Name}' require argument '{argument.Name}' "
                            + "must map to a non-empty string representing the field path.");
                    }

                    requires[argument.Name] = path;
                }

                if (requires.Count > 0)
                {
                    fields[field.Name] = requires;
                }
            }

            if (fields.Count > 0)
            {
                entityRequires[entityType.Name] = new EntityRequiresInfo
                {
                    Fields = fields
                };
            }
        }

        return entityRequires;
    }

    private static Dictionary<string, string> ParseArguments(
        string schemaName,
        string lookupName,
        JsonElement lookup)
    {
        if (!lookup.TryGetProperty("arguments", out var argumentsElement)
            || argumentsElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var arguments = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var argument in argumentsElement.EnumerateObject())
        {
            arguments[argument.Name] = ReadArgumentPath(schemaName, lookupName, argument);
        }

        return arguments;
    }

    private static string ReadArgumentPath(
        string schemaName,
        string lookupName,
        JsonProperty argument)
    {
        switch (argument.Value.ValueKind)
        {
            case JsonValueKind.String:
                // An empty-string path is the "splat" marker that tells the
                // connector to spread the variable value's object fields into
                // the representation root (used for wrapper-shape arguments on
                // nested '@key' lookups). Any other string is a path segment.
                return argument.Value.GetString() ?? string.Empty;

            case JsonValueKind.Object:
                if (argument.Value.TryGetProperty("path", out var pathProperty)
                    && pathProperty.ValueKind == JsonValueKind.String)
                {
                    return pathProperty.GetString() ?? string.Empty;
                }

                break;
        }

        throw new InvalidOperationException(
            $"Source schema '{schemaName}' lookup '{lookupName}' argument '{argument.Name}' "
            + "must map to a string key field path or an object with a 'path' string property.");
    }
}
