using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Logging.Contracts;

namespace HotChocolate.Fusion;

/// <summary>
/// Composes source schema settings into gateway settings for a specific environment
/// </summary>
internal sealed partial class SettingsComposer
{
    private const string TransportsPropertyName = "transports";
    private const string HttpTransportName = "http";
    private const string WebSocketsTransportName = "websockets";
    private const string UrlPropertyName = "url";
    private const string DevUrlPropertyName = "devUrl";

    private static readonly Regex s_variablePattern = VariablePatternRegex();

    /// <summary>
    /// Composes multiple source schema settings into gateway settings for the specified environment
    /// </summary>
    /// <param name="gatewaySettings">Buffer to write the composed gateway settings to</param>
    /// <param name="sourceSchemaSettings">Source schema settings documents to compose</param>
    /// <param name="environment">
    /// Target environment for variable resolution. A source schema that is not part of the local
    /// development environment resolves against
    /// <see cref="SettingsComposerOptions.ExternalEnvironment"/> instead.
    /// </param>
    /// <param name="options">Options that control how source schema URLs are resolved</param>
    /// <param name="compositionLog">Log that receives the composition diagnostics</param>
    public void Compose(
        IBufferWriter<byte> gatewaySettings,
        ReadOnlySpan<JsonElement> sourceSchemaSettings,
        string environment,
        SettingsComposerOptions options,
        ICompositionLog compositionLog)
    {
        ArgumentNullException.ThrowIfNull(gatewaySettings);
        ArgumentException.ThrowIfNullOrEmpty(environment);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(compositionLog);

        if (sourceSchemaSettings.IsEmpty)
        {
            throw new ArgumentException(
                "At least one source schema settings document is required",
                nameof(sourceSchemaSettings));
        }

        using var writer = new Utf8JsonWriter(gatewaySettings, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WritePropertyName("sourceSchemas");
        writer.WriteStartObject();

        foreach (var sourceSchema in sourceSchemaSettings)
        {
            ComposeSourceSchema(writer, sourceSchema, environment, options, compositionLog);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
    }

    /// <summary>
    /// Resolves the variables in the specified value against the variables that the source schema
    /// settings define for the specified environment.
    /// </summary>
    /// <param name="value">The value that may contain variable references</param>
    /// <param name="sourceSchemaSettings">The source schema settings document</param>
    /// <param name="environment">Target environment for variable resolution</param>
    /// <param name="resolvedValue">The value with all variable references replaced</param>
    /// <returns>
    /// <c>true</c> if all variable references could be resolved; otherwise <c>false</c>.
    /// </returns>
    public static bool TryResolveVariables(
        string value,
        JsonElement sourceSchemaSettings,
        string environment,
        [NotNullWhen(true)] out string? resolvedValue)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrEmpty(environment);

        var environmentVariables = ExtractEnvironmentVariables(sourceSchemaSettings, environment);

        return TryResolveVariablesInString(value, environmentVariables, out resolvedValue);
    }

    private static void ComposeSourceSchema(
        Utf8JsonWriter writer,
        JsonElement settings,
        string environment,
        SettingsComposerOptions options,
        ICompositionLog compositionLog)
    {
        // first we will get the source schema name.
        if (!settings.TryGetProperty("name", out var nameElement))
        {
            throw new InvalidOperationException("Source schema missing required 'name' property");
        }

        var schemaName = nameElement.GetString();
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new InvalidOperationException("Source schema 'name' property cannot be empty");
        }

        // a source schema that does not belong to the local development environment resolves
        // its settings against the environment that the external configuration was built for.
        var effectiveEnvironment =
            options.ExternalEnvironment is { } externalEnvironment
            && !options.LocalSourceSchemas.Contains(schemaName)
                ? externalEnvironment
                : environment;

        // next we collect the variables
        var environmentVariables = ExtractEnvironmentVariables(settings, effectiveEnvironment);

        // a source schema that is backed by a resource of the local development environment
        // is reached through the URL of that resource instead of its configured URL.
        options.LocalUrlOverrides.TryGetValue(schemaName, out var localUrlOverride);

        var context = new SourceSchemaContext(
            schemaName,
            effectiveEnvironment,
            environmentVariables,
            localUrlOverride,
            options.PreferDevUrls,
            compositionLog);

        // now that we have all the context in memory we can start with the settings composition.
        writer.WritePropertyName(schemaName);
        writer.WriteStartObject();

        var hasTransports = false;

        foreach (var property in settings.EnumerateObject())
        {
            // when we compose the settings file we will skip the name and the environments.
            // the environments only exist in the source schema and will not survive the composition.
            if (property.Name is "name" or "environments")
            {
                continue;
            }

            if (property.Name is TransportsPropertyName)
            {
                hasTransports = true;

                if (property.Value.ValueKind is JsonValueKind.Object)
                {
                    writer.WritePropertyName(property.Name);
                    WriteTransports(writer, property.Value, context);
                    continue;
                }
            }

            writer.WritePropertyName(property.Name);
            WriteJsonElementWithVariableSubstitution(writer, property.Value, environmentVariables);
        }

        // the local URL must reach the gateway configuration even when the source schema
        // settings do not configure a transport at all.
        if (!hasTransports && localUrlOverride is not null)
        {
            writer.WritePropertyName(TransportsPropertyName);
            writer.WriteStartObject();
            writer.WritePropertyName(HttpTransportName);
            WriteHttpTransport(writer, localUrlOverride);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WriteTransports(
        Utf8JsonWriter writer,
        JsonElement transports,
        in SourceSchemaContext context)
    {
        writer.WriteStartObject();

        var hasHttpTransport = false;

        foreach (var property in transports.EnumerateObject())
        {
            var isHttpTransport = property.Name is HttpTransportName;
            hasHttpTransport |= isHttpTransport;

            writer.WritePropertyName(property.Name);

            if (property.Value.ValueKind is JsonValueKind.Object
                && property.Name is HttpTransportName or WebSocketsTransportName)
            {
                WriteTransport(writer, property.Value, isHttpTransport, context);
                continue;
            }

            WriteJsonElementWithVariableSubstitution(
                writer,
                property.Value,
                context.EnvironmentVariables);
        }

        if (!hasHttpTransport && context.LocalUrlOverride is not null)
        {
            writer.WritePropertyName(HttpTransportName);
            WriteHttpTransport(writer, context.LocalUrlOverride);
        }

        writer.WriteEndObject();
    }

    private static void WriteHttpTransport(Utf8JsonWriter writer, string url)
    {
        writer.WriteStartObject();
        writer.WriteString(UrlPropertyName, url);
        writer.WriteEndObject();
    }

    private static void WriteTransport(
        Utf8JsonWriter writer,
        JsonElement transport,
        bool isHttpTransport,
        in SourceSchemaContext context)
    {
        var resolvedUrl = ResolveTransportUrl(transport, isHttpTransport, context);
        var hasUrl = false;

        writer.WriteStartObject();

        foreach (var property in transport.EnumerateObject())
        {
            // the development URL is a source schema concern and never survives the composition.
            if (property.Name is DevUrlPropertyName)
            {
                continue;
            }

            if (property.Name is UrlPropertyName && resolvedUrl is not null)
            {
                writer.WriteString(UrlPropertyName, resolvedUrl);
                hasUrl = true;
                continue;
            }

            writer.WritePropertyName(property.Name);
            WriteJsonElementWithVariableSubstitution(
                writer,
                property.Value,
                context.EnvironmentVariables);
        }

        if (!hasUrl && resolvedUrl is not null)
        {
            writer.WriteString(UrlPropertyName, resolvedUrl);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Determines the URL of a transport. Returns <c>null</c> when the configured <c>url</c> is
    /// composed as it is.
    /// </summary>
    private static string? ResolveTransportUrl(
        JsonElement transport,
        bool isHttpTransport,
        in SourceSchemaContext context)
    {
        if (context.LocalUrlOverride is { } localUrlOverride)
        {
            return isHttpTransport ? localUrlOverride : null;
        }

        if (!context.PreferDevUrls)
        {
            return null;
        }

        var url = GetStringPropertyOrNull(transport, UrlPropertyName);
        var devUrl = GetDevUrlOrNull(transport);

        if (devUrl is not null)
        {
            if (TryResolveVariablesInString(devUrl, context.EnvironmentVariables, out var resolved))
            {
                return resolved;
            }

            WriteUnresolvedUrlWarning(context, DevUrlPropertyName);

            // an unresolvable development URL falls back to the configured URL.
            if (url is null)
            {
                return devUrl;
            }
        }
        else if (url is not null && isHttpTransport)
        {
            WriteDevUrlMissingWarning(context);
        }

        if (url is null)
        {
            return null;
        }

        if (TryResolveVariablesInString(url, context.EnvironmentVariables, out var resolvedUrl))
        {
            return resolvedUrl;
        }

        WriteUnresolvedUrlWarning(context, UrlPropertyName);

        return url;
    }

    private static void WriteDevUrlMissingWarning(in SourceSchemaContext context)
    {
        context.CompositionLog.Write(
            LogEntryBuilder.New()
                .SetMessage(
                    "The source schema '{0}' does not specify a 'devUrl' for its HTTP transport. "
                    + "The composed configuration uses its 'url', which might not be reachable "
                    + "from the local development environment.",
                    context.SchemaName)
                .SetCode(LogEntryCodes.SourceSchemaDevUrlMissing)
                .SetSeverity(LogSeverity.Warning)
                .Build());
    }

    private static void WriteUnresolvedUrlWarning(
        in SourceSchemaContext context,
        string propertyName)
    {
        var fallback = propertyName is DevUrlPropertyName
            ? "The 'url' is used instead."
            : "The configured value is composed as it is.";

        context.CompositionLog.Write(
            LogEntryBuilder.New()
                .SetMessage(
                    "The '{0}' of the source schema '{1}' contains variables that are not defined "
                    + "for the environment '{2}'. {3}",
                    propertyName,
                    context.SchemaName,
                    context.Environment,
                    fallback)
                .SetCode(LogEntryCodes.SourceSchemaUrlVariableUnresolved)
                .SetSeverity(LogSeverity.Warning)
                .Build());
    }

    /// <summary>
    /// Gets the development URL of a transport. A development URL that is not set or blank counts
    /// as not defined, which lets the resolution fall through to the configured <c>url</c>.
    /// </summary>
    private static string? GetDevUrlOrNull(JsonElement transport)
    {
        var devUrl = GetStringPropertyOrNull(transport, DevUrlPropertyName);

        return string.IsNullOrWhiteSpace(devUrl) ? null : devUrl;
    }

    private static string? GetStringPropertyOrNull(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value)
            && value.ValueKind is JsonValueKind.String
                ? value.GetString()
                : null;

    private static Dictionary<string, JsonElement> ExtractEnvironmentVariables(
        JsonElement settings,
        string environment)
    {
        var variables = new Dictionary<string, JsonElement>();

        if (settings.ValueKind is JsonValueKind.Object
            && settings.TryGetProperty("environments", out var environmentsElement)
            && environmentsElement.ValueKind is JsonValueKind.Object
            && environmentsElement.TryGetProperty(environment, out var targetEnvElement)
            && targetEnvElement.ValueKind is JsonValueKind.Object)
        {
            foreach (var variable in targetEnvElement.EnumerateObject())
            {
                variables[variable.Name] = variable.Value;
            }
        }

        return variables;
    }

    private static void WriteJsonElementWithVariableSubstitution(
        Utf8JsonWriter writer,
        JsonElement element,
        Dictionary<string, JsonElement> environmentVariables)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteJsonElementWithVariableSubstitution(writer, property.Value, environmentVariables);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteJsonElementWithVariableSubstitution(writer, item, environmentVariables);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                var stringValue = element.GetString()!;

                // Check if this is a pure variable reference (e.g., "{{ENABLED}}")
                if (IsPureVariableReference(stringValue, out var variableName))
                {
                    // Write the variable's actual value with its original type
                    if (!environmentVariables.TryGetValue(variableName, out var variableValue))
                    {
                        throw new InvalidOperationException($"Variable '{variableName}' not found in environment");
                    }

                    WriteJsonElementWithVariableSubstitution(writer, variableValue, environmentVariables);
                }
                else
                {
                    // Perform string interpolation for mixed content
                    var resolvedValue = ResolveVariablesInString(stringValue, environmentVariables);
                    writer.WriteStringValue(resolvedValue);
                }
                break;

            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                {
                    writer.WriteNumberValue(intValue);
                }
                else if (element.TryGetInt64(out var longValue))
                {
                    writer.WriteNumberValue(longValue);
                }
                else if (element.TryGetDouble(out var doubleValue))
                {
                    writer.WriteNumberValue(doubleValue);
                }
                else
                {
                    writer.WriteRawValue(element.GetRawText());
                }
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;

            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }

    private static bool IsPureVariableReference(string input, out string variableName)
    {
        var match = s_variablePattern.Match(input);
        if (match.Success && match.Value == input)
        {
            variableName = match.Groups[1].Value;
            return true;
        }

        variableName = string.Empty;
        return false;
    }

    private static string ResolveVariablesInString(
        string input,
        Dictionary<string, JsonElement> environmentVariables)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return s_variablePattern.Replace(input, match =>
        {
            var variableName = match.Groups[1].Value;

            if (environmentVariables.TryGetValue(variableName, out var variableValue))
            {
                return FormatVariableValue(variableValue);
            }

            throw new InvalidOperationException($"Variable '{variableName}' not found in environment");
        });
    }

    private static bool TryResolveVariablesInString(
        string input,
        Dictionary<string, JsonElement> environmentVariables,
        [NotNullWhen(true)] out string? resolvedValue)
    {
        if (string.IsNullOrEmpty(input))
        {
            resolvedValue = input;
            return true;
        }

        var isResolved = true;

        var result = s_variablePattern.Replace(input, match =>
        {
            var variableName = match.Groups[1].Value;

            if (environmentVariables.TryGetValue(variableName, out var variableValue))
            {
                return FormatVariableValue(variableValue);
            }

            isResolved = false;

            return match.Value;
        });

        resolvedValue = isResolved ? result : null;

        return isResolved;
    }

    private static string FormatVariableValue(JsonElement variableValue)
        => variableValue.ValueKind switch
        {
            JsonValueKind.String => variableValue.GetString()!,
            JsonValueKind.Number => variableValue.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => variableValue.GetRawText()
        };

    [GeneratedRegex(@"\{\{([a-zA-Z0-9_-]+)\}\}")]
    private static partial Regex VariablePatternRegex();

    private readonly record struct SourceSchemaContext(
        string SchemaName,
        string Environment,
        Dictionary<string, JsonElement> EnvironmentVariables,
        string? LocalUrlOverride,
        bool PreferDevUrls,
        ICompositionLog CompositionLog);
}
