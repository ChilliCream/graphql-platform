using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace HotChocolate.Fusion.Packaging.Serializers;

internal static class RegoPolicyBundleManifestSerializer
{
    // A fixed writer configuration keeps the serialized bytes reproducible for identical content.
    private static readonly JsonWriterOptions s_writerOptions = new() { Indented = false };

    public static void Format(RegoPolicyBundleManifest manifest, IBufferWriter<byte> writer)
    {
        using var jsonWriter = new Utf8JsonWriter(writer, s_writerOptions);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteNumber("formatVersion", manifest.FormatVersion);

        jsonWriter.WriteStartArray("policies");

        foreach (var policy in manifest.Policies)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("name", policy.Name);
            jsonWriter.WriteString("package", policy.Package);
            jsonWriter.WriteString("entrypoint", policy.Entrypoint);

            jsonWriter.WriteStartArray("modules");
            foreach (var module in policy.Modules)
            {
                jsonWriter.WriteStringValue(module);
            }
            jsonWriter.WriteEndArray();

            if (policy.Requirements is null)
            {
                jsonWriter.WriteNull("requirements");
            }
            else
            {
                jsonWriter.WriteString("requirements", policy.Requirements);
            }

            jsonWriter.WriteStartObject("sha256");
            foreach (var entry in policy.Sha256.OrderBy(t => t.Key, StringComparer.Ordinal))
            {
                jsonWriter.WriteString(entry.Key, entry.Value);
            }
            jsonWriter.WriteEndObject();

            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndArray();

        jsonWriter.WriteStartArray("libraries");
        foreach (var library in manifest.Libraries)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("path", library.Path);
            jsonWriter.WriteString("sha256", library.Sha256);
            jsonWriter.WriteEndObject();
        }
        jsonWriter.WriteEndArray();

        if (manifest.Data is null)
        {
            jsonWriter.WriteNull("data");
        }
        else
        {
            jsonWriter.WriteStartObject("data");
            jsonWriter.WriteString("path", manifest.Data.Path);
            jsonWriter.WriteString("sha256", manifest.Data.Sha256);
            jsonWriter.WriteEndObject();
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();
    }

    public static RegoPolicyBundleManifest Parse(ReadOnlyMemory<byte> manifestBytes)
    {
        using var document = JsonDocument.Parse(manifestBytes);
        var root = document.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("Invalid Rego policy bundle manifest format.");
        }

        if (!root.TryGetProperty("formatVersion", out var formatVersionProp)
            || formatVersionProp.ValueKind is not JsonValueKind.Number)
        {
            throw new JsonException("The Rego policy bundle manifest must contain a formatVersion property.");
        }

        if (!root.TryGetProperty("policies", out var policiesProp) || policiesProp.ValueKind is not JsonValueKind.Array)
        {
            throw new JsonException("The Rego policy bundle manifest must contain a policies array.");
        }

        var policies = ImmutableArray.CreateBuilder<RegoPolicyBundleManifestPolicy>();

        foreach (var policyElement in policiesProp.EnumerateArray())
        {
            policies.Add(ParsePolicy(policyElement));
        }

        if (!root.TryGetProperty("libraries", out var librariesProp)
            || librariesProp.ValueKind is not JsonValueKind.Array)
        {
            throw new JsonException("The Rego policy bundle manifest must contain a libraries array.");
        }

        var libraries = ImmutableArray.CreateBuilder<RegoPolicyBundleManifestFile>();

        foreach (var libraryElement in librariesProp.EnumerateArray())
        {
            libraries.Add(ParseFile(libraryElement, "library"));
        }

        RegoPolicyBundleManifestFile? data = null;

        if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind is not JsonValueKind.Null)
        {
            data = ParseFile(dataProp, "data");
        }

        return new RegoPolicyBundleManifest
        {
            FormatVersion = formatVersionProp.GetInt32(),
            Policies = policies.ToImmutable(),
            Libraries = libraries.ToImmutable(),
            Data = data
        };
    }

    private static RegoPolicyBundleManifestPolicy ParsePolicy(JsonElement element)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException("Invalid Rego policy bundle manifest policy entry.");
        }

        var name = GetRequiredString(element, "name");
        var package = GetRequiredString(element, "package");
        var entrypoint = GetRequiredString(element, "entrypoint");

        if (!element.TryGetProperty("modules", out var modulesProp) || modulesProp.ValueKind is not JsonValueKind.Array)
        {
            throw new JsonException($"The Rego policy bundle manifest policy '{name}' must contain a modules array.");
        }

        var modules = ImmutableArray.CreateBuilder<string>();
        foreach (var moduleElement in modulesProp.EnumerateArray())
        {
            modules.Add(moduleElement.GetString() ?? throw new JsonException("Invalid module path."));
        }

        string? requirements = null;
        if (element.TryGetProperty("requirements", out var requirementsProp)
            && requirementsProp.ValueKind is not JsonValueKind.Null)
        {
            requirements = requirementsProp.GetString()
                ?? throw new JsonException("Invalid requirements path.");
        }

        if (!element.TryGetProperty("sha256", out var sha256Prop) || sha256Prop.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException($"The Rego policy bundle manifest policy '{name}' must contain a sha256 object.");
        }

        var sha256 = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        foreach (var entry in sha256Prop.EnumerateObject())
        {
            var digest = entry.Value.GetString() ?? throw new JsonException("Invalid digest.");

            if (!sha256.ContainsKey(entry.Name))
            {
                sha256.Add(entry.Name, digest);
                continue;
            }

            throw new JsonException(
                $"The Rego policy bundle manifest policy '{name}' lists the sha256 key '{entry.Name}' "
                + "more than once.");
        }

        return new RegoPolicyBundleManifestPolicy
        {
            Name = name,
            Package = package,
            Entrypoint = entrypoint,
            Modules = modules.ToImmutable(),
            Requirements = requirements,
            Sha256 = sha256.ToImmutable()
        };
    }

    private static RegoPolicyBundleManifestFile ParseFile(JsonElement element, string kind)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            throw new JsonException($"Invalid Rego policy bundle manifest {kind} entry.");
        }

        return new RegoPolicyBundleManifestFile
        {
            Path = GetRequiredString(element, "path"),
            Sha256 = GetRequiredString(element, "sha256")
        };
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop) || prop.ValueKind is not JsonValueKind.String)
        {
            throw new JsonException($"The '{propertyName}' property must be a string.");
        }

        return prop.GetString()!;
    }
}
