using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion;

internal static class ApolloFederationSourceSchemaSettings
{
    private const string ExtensionsPath = "$.extensions";
    private const string ChilliCreamPath = "$.extensions.chillicream";
    private const string SupportPath = "$.extensions.chillicream.apolloFederationSupport";

    internal const string VersionPath =
        "$.extensions.chillicream.apolloFederationSupport.version";

    public static bool TryReadVersion(
        string sourceSchemaName,
        JsonElement settings,
        out ApolloFederationVersion? version,
        [NotNullWhen(false)] out string? errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);

        version = null;
        errorMessage = null;

        if (settings.ValueKind is not JsonValueKind.Object)
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_SettingMustBeObject,
                "$",
                sourceSchemaName,
                settings.ValueKind);
            return false;
        }

        if (!settings.TryGetProperty("extensions", out var extensions))
        {
            return true;
        }

        if (extensions.ValueKind is not JsonValueKind.Object)
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_SettingMustBeObject,
                ExtensionsPath,
                sourceSchemaName,
                extensions.ValueKind);
            return false;
        }

        if (!extensions.TryGetProperty("chillicream", out var chilliCream))
        {
            return true;
        }

        if (chilliCream.ValueKind is not JsonValueKind.Object)
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_SettingMustBeObject,
                ChilliCreamPath,
                sourceSchemaName,
                chilliCream.ValueKind);
            return false;
        }

        if (!chilliCream.TryGetProperty("apolloFederationSupport", out var support))
        {
            return true;
        }

        if (support.ValueKind is not JsonValueKind.Object)
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_SettingMustBeObject,
                SupportPath,
                sourceSchemaName,
                support.ValueKind);
            return false;
        }

        using var properties = support.EnumerateObject();

        if (!properties.MoveNext()
            || !properties.Current.NameEquals("version")
            || properties.MoveNext())
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_InvalidApolloFederationSupportShape,
                SupportPath,
                sourceSchemaName);
            return false;
        }

        var versionElement = support.GetProperty("version");

        if (versionElement.ValueKind is not JsonValueKind.String)
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_SettingMustBeString,
                VersionPath,
                sourceSchemaName,
                versionElement.ValueKind);
            return false;
        }

        var versionValue = versionElement.GetString();

        version = versionValue switch
        {
            "1.0" => ApolloFederationVersion.Version1,
            "2.0" => ApolloFederationVersion.Version2,
            _ => null
        };

        if (version is null)
        {
            errorMessage = string.Format(
                SourceSchemaSettingsReader_UnsupportedApolloFederationVersion,
                VersionPath,
                sourceSchemaName,
                versionValue);
            return false;
        }

        return true;
    }
}
