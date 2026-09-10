using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

internal static class SourceSchemaSettingsReader
{
    public static bool TryRead(
        string sourceSchemaName,
        JsonDocument sourceSchemaSettings,
        ICompositionLog compositionLog,
        out SourceSchemaSettingsReadResult result)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);
        ArgumentNullException.ThrowIfNull(sourceSchemaSettings);
        ArgumentNullException.ThrowIfNull(compositionLog);

        result = default;

        if (!ApolloFederationSourceSchemaSettings.TryReadVersion(
            sourceSchemaName,
            sourceSchemaSettings.RootElement,
            out var version,
            out var errorMessage))
        {
            compositionLog.Write(
                LogEntryBuilder.New()
                    .SetMessage(errorMessage)
                    .SetCode(LogEntryCodes.InvalidApolloFederationSupportSettings)
                    .SetSeverity(LogSeverity.Error)
                    .Build());
            return false;
        }

        var schemaSettings =
            sourceSchemaSettings.Deserialize(SettingsJsonSerializerContext.Default.SourceSchemaSettings)!;
        var options = schemaSettings.ToOptions();

        options.IsApolloFederationV1 = version is ApolloFederationVersion.Version1;
        result = new(
            schemaSettings,
            options,
            version is null ? null : RemoveApolloFederationSupport(sourceSchemaSettings));
        return true;
    }

    private static JsonDocument RemoveApolloFederationSupport(JsonDocument sourceSchemaSettings)
    {
        var root = JsonNode.Parse(sourceSchemaSettings.RootElement.GetRawText())!.AsObject();
        var extensions = root["extensions"]!.AsObject();
        var chilliCream = extensions["chillicream"]!.AsObject();

        chilliCream.Remove("apolloFederationSupport");

        if (chilliCream.Count == 0)
        {
            extensions.Remove("chillicream");
        }

        if (extensions.Count == 0)
        {
            root.Remove("extensions");
        }

        using var buffer = new PooledArrayWriter();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            root.WriteTo(writer);
            writer.Flush();
        }

        return JsonDocument.Parse(buffer.WrittenMemory.ToArray());
    }
}

internal readonly record struct SourceSchemaSettingsReadResult(
    SourceSchemaSettings Settings,
    SourceSchemaOptions Options,
    JsonDocument? RuntimeSettings);
