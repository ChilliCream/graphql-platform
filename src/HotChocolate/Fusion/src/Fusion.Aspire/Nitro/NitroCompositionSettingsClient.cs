using System.Text.Json;
using HotChocolate.Fusion.Options;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

internal sealed class NitroCompositionSettingsClient(GraphQLHttpClient client)
    : INitroCompositionSettingsClient
{
    public async Task<CompositionSettings?> GetAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        var variables = new Dictionary<string, object?>
        {
            ["apiId"] = apiId,
            ["stageName"] = stage
        };
        var request = new GraphQLHttpRequest(
#if NITRO_PERSISTED_OPERATIONS
            new OperationRequest(
                id: NitroOperationDocuments.GetCompositionSettingsOperationId(),
                operationName: NitroOperationDocuments.GetCompositionSettingsOperationName,
                variables: variables),
#else
            new OperationRequest(
                NitroOperationDocuments.GetCompositionSettingsDocument(),
                operationName: NitroOperationDocuments.GetCompositionSettingsOperationName,
                variables: variables),
#endif
            connection.GraphQLEndpoint)
        {
            OnMessageCreated = (_, requestMessage, _) =>
                NitroRequestHeaders.Apply(requestMessage, connection.Credential)
        };

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var result = await response.ReadAsResultAsync(cancellationToken);
        if (result.Errors.ValueKind is JsonValueKind.Array
            && result.Errors.GetArrayLength() > 0)
        {
            throw new NitroOperationException(
                "Nitro returned GraphQL errors for the composition settings operation: "
                + (ReadErrorMessage(result.Errors) ?? "no message."),
                ReadErrorCode(result.Errors));
        }

        if (result.Data.ValueKind is not JsonValueKind.Object
            || !result.Data.TryGetProperty("node", out var node))
        {
            throw new InvalidDataException(
                "Nitro returned malformed composition settings.");
        }

        if (node.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidOperationException(
                $"Nitro knows no api with the id '{apiId}'. Check the api id that is passed to "
                + "WithNitroApiId.");
        }

        if (node.ValueKind is not JsonValueKind.Object
            || !node.TryGetProperty("stage", out var stageElement)
            || stageElement.ValueKind is JsonValueKind.Null)
        {
            throw new InvalidOperationException(
                $"The api with the id '{apiId}' has no stage named '{stage}' in Nitro.");
        }

        if (stageElement.ValueKind is not JsonValueKind.Object
            || !stageElement.TryGetProperty("compositionSettings", out var settings)
            || settings.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        if (settings.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "Nitro returned malformed composition settings.");
        }

        return new CompositionSettings
        {
            Preprocessor = new CompositionSettings.PreprocessorSettings
            {
                ExcludeByTag = ReadStringSet(settings, "excludeByTag")
            },
            Merger = new CompositionSettings.MergerSettings
            {
                CacheControlMergeBehavior = ReadDirectiveMergeBehavior(
                    settings,
                    "cacheControlMergeBehavior"),
                EnableGlobalObjectIdentification = ReadBoolean(
                    settings,
                    "enableGlobalObjectIdentification"),
                NodeResolution = ReadNodeResolution(settings, "nodeResolution"),
                RemoveUnreferencedDefinitions = ReadBoolean(
                    settings,
                    "removeUnreferencedDefinitions"),
                TagMergeBehavior = ReadDirectiveMergeBehavior(settings, "tagMergeBehavior")
            }
        };
    }

    private static string? ReadErrorMessage(JsonElement errors)
        => errors[0].ValueKind is JsonValueKind.Object
            && errors[0].TryGetProperty("message", out var message)
            && message.ValueKind is JsonValueKind.String
                ? message.GetString()
                : null;

    private static string? ReadErrorCode(JsonElement errors)
        => errors[0].ValueKind is JsonValueKind.Object
            && errors[0].TryGetProperty("extensions", out var extensions)
            && extensions.ValueKind is JsonValueKind.Object
            && extensions.TryGetProperty("code", out var code)
            && code.ValueKind is JsonValueKind.String
                ? code.GetString()
                : null;

    private static bool? ReadBoolean(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidDataException(
                $"Nitro returned an invalid value for '{propertyName}'.")
        };
    }

    private static HashSet<string>? ReadStringSet(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind is not JsonValueKind.Array)
        {
            throw new InvalidDataException(
                $"Nitro returned an invalid value for '{propertyName}'.");
        }

        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind is not JsonValueKind.String)
            {
                throw new InvalidDataException(
                    $"Nitro returned an invalid value for '{propertyName}'.");
            }

            values.Add(item.GetString()!);
        }

        return values;
    }

    private static DirectiveMergeBehavior? ReadDirectiveMergeBehavior(
        JsonElement parent,
        string propertyName)
    {
        var value = ReadEnum(parent, propertyName);
        return value switch
        {
            null => null,
            "IGNORE" => DirectiveMergeBehavior.Ignore,
            "INCLUDE" => DirectiveMergeBehavior.Include,
            "INCLUDE_PRIVATE" => DirectiveMergeBehavior.IncludePrivate,
            _ => throw new InvalidDataException(
                $"Nitro returned an invalid value for '{propertyName}'.")
        };
    }

    private static NodeResolution? ReadNodeResolution(JsonElement parent, string propertyName)
    {
        var value = ReadEnum(parent, propertyName);
        return value switch
        {
            null => null,
            "GATEWAY" => NodeResolution.Gateway,
            "SOURCE_SCHEMA" => NodeResolution.SourceSchema,
            _ => throw new InvalidDataException(
                $"Nitro returned an invalid value for '{propertyName}'.")
        };
    }

    private static string? ReadEnum(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value)
            || value.ValueKind is JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind is JsonValueKind.String
            ? value.GetString()
            : throw new InvalidDataException(
                $"Nitro returned an invalid value for '{propertyName}'.");
    }
}
