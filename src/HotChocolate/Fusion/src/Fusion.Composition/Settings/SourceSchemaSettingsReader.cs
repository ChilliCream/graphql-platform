using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion;

internal static class SourceSchemaSettingsReader
{
    private const string ExtensionsPath = "$.extensions";
    private const string ChilliCreamPath = "$.extensions.chillicream";
    private const string ApolloFederationSupportPath =
        "$.extensions.chillicream.apolloFederationSupport";
    private const string ApolloFederationVersionPath =
        "$.extensions.chillicream.apolloFederationSupport.version";

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

        var root = sourceSchemaSettings.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            WriteMustBeObject(compositionLog, sourceSchemaName, "$", root.ValueKind);
            return false;
        }

        var schemaSettings =
            sourceSchemaSettings.Deserialize(SettingsJsonSerializerContext.Default.SourceSchemaSettings)!;
        var options = schemaSettings.ToOptions();

        if (!root.TryGetProperty("extensions", out var extensions))
        {
            result = new(schemaSettings, options, null);
            return true;
        }

        if (extensions.ValueKind is not JsonValueKind.Object)
        {
            WriteMustBeObject(
                compositionLog,
                sourceSchemaName,
                ExtensionsPath,
                extensions.ValueKind);
            return false;
        }

        if (!extensions.TryGetProperty("chillicream", out var chilliCream))
        {
            result = new(schemaSettings, options, null);
            return true;
        }

        if (chilliCream.ValueKind is not JsonValueKind.Object)
        {
            WriteMustBeObject(
                compositionLog,
                sourceSchemaName,
                ChilliCreamPath,
                chilliCream.ValueKind);
            return false;
        }

        if (!chilliCream.TryGetProperty(
            "apolloFederationSupport",
            out var apolloFederationSupport))
        {
            result = new(schemaSettings, options, null);
            return true;
        }

        if (apolloFederationSupport.ValueKind is not JsonValueKind.Object)
        {
            WriteMustBeObject(
                compositionLog,
                sourceSchemaName,
                ApolloFederationSupportPath,
                apolloFederationSupport.ValueKind);
            return false;
        }

        using var properties = apolloFederationSupport.EnumerateObject();

        if (!properties.MoveNext()
            || !properties.Current.NameEquals("version")
            || properties.MoveNext())
        {
            compositionLog.Write(
                LogEntryBuilder.New()
                    .SetMessage(
                        SourceSchemaSettingsReader_InvalidApolloFederationSupportShape,
                        ApolloFederationSupportPath,
                        sourceSchemaName)
                    .SetCode(LogEntryCodes.InvalidApolloFederationSupportSettings)
                    .SetSeverity(LogSeverity.Error)
                    .Build());
            return false;
        }

        var version = apolloFederationSupport.GetProperty("version");

        if (version.ValueKind is not JsonValueKind.String)
        {
            compositionLog.Write(
                LogEntryBuilder.New()
                    .SetMessage(
                        SourceSchemaSettingsReader_SettingMustBeString,
                        ApolloFederationVersionPath,
                        sourceSchemaName,
                        version.ValueKind)
                    .SetCode(LogEntryCodes.InvalidApolloFederationSupportSettings)
                    .SetSeverity(LogSeverity.Error)
                    .Build());
            return false;
        }

        var versionValue = version.GetString()!;

        if (!versionValue.Equals("1.0", StringComparison.Ordinal))
        {
            compositionLog.Write(
                LogEntryBuilder.New()
                    .SetMessage(
                        SourceSchemaSettingsReader_UnsupportedApolloFederationVersion,
                        ApolloFederationVersionPath,
                        sourceSchemaName,
                        versionValue)
                    .SetCode(LogEntryCodes.InvalidApolloFederationSupportSettings)
                    .SetSeverity(LogSeverity.Error)
                    .Build());
            return false;
        }

        options.IsApolloFederationV1 = true;
        result = new(
            schemaSettings,
            options,
            RemoveApolloFederationSupport(sourceSchemaSettings));
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

    private static void WriteMustBeObject(
        ICompositionLog compositionLog,
        string sourceSchemaName,
        string path,
        JsonValueKind actualKind)
    {
        compositionLog.Write(
            LogEntryBuilder.New()
                .SetMessage(
                    SourceSchemaSettingsReader_SettingMustBeObject,
                    path,
                    sourceSchemaName,
                    actualKind)
                .SetCode(LogEntryCodes.InvalidApolloFederationSupportSettings)
                .SetSeverity(LogSeverity.Error)
                .Build());
    }
}

internal readonly record struct SourceSchemaSettingsReadResult(
    SourceSchemaSettings Settings,
    SourceSchemaOptions Options,
    JsonDocument? RuntimeSettings);
