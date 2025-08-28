using System.Buffers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HotChocolate.Fusion;

/// <summary>
/// Composes source schema settings into gateway settings for a specific environment
/// </summary>
public sealed class SettingsComposer
{
    private static readonly Regex s_variablePattern = new(@"\{\{([a-zA-Z0-9_-]+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Composes multiple source schema settings into gateway settings for the specified environment
    /// </summary>
    /// <param name="gatewaySettings">Buffer to write the composed gateway settings to</param>
    /// <param name="sourceSchemaSettings">Source schema settings documents to compose</param>
    /// <param name="environment">Target environment for variable resolution</param>
    public void Compose(
        IBufferWriter<byte> gatewaySettings,
        ReadOnlySpan<JsonElement> sourceSchemaSettings,
        string environment)
    {
        ArgumentNullException.ThrowIfNull(gatewaySettings);
        ArgumentException.ThrowIfNullOrEmpty(environment);

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
            ComposeSourceSchema(writer, sourceSchema, environment);
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
    }

    private static void ComposeSourceSchema(
        Utf8JsonWriter writer,
        JsonElement settings,
        string environment)
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

        // next we collect the variables
        var environmentVariables = ExtractEnvironmentVariables(settings, environment);

        // now that we have all the context in memory we can start with the settings composition.
        writer.WritePropertyName(schemaName);
        writer.WriteStartObject();

        foreach (var property in settings.EnumerateObject())
        {
            // when we compose the settings file we will skip the name and the environments.
            // the environments only exist in the source schema and will not survive the composition.
            if (property.Name is "name" or "environments")
            {
                continue;
            }

            writer.WritePropertyName(property.Name);
            WriteJsonElementWithVariableSubstitution(writer, property.Value, environmentVariables);
        }

        writer.WriteEndObject();
    }

    private static Dictionary<string, JsonElement> ExtractEnvironmentVariables(
        JsonElement settings,
        string environment)
    {
        var variables = new Dictionary<string, JsonElement>();

        if (settings.TryGetProperty("environments", out var environmentsElement)
            && environmentsElement.TryGetProperty(environment, out var targetEnvElement))
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
                return variableValue.ValueKind switch
                {
                    JsonValueKind.String => variableValue.GetString()!,
                    JsonValueKind.Number => variableValue.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "null",
                    _ => variableValue.GetRawText()
                };
            }

            throw new InvalidOperationException($"Variable '{variableName}' not found in environment");
        });
    }
}
