using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ChilliCream.Nitro.CommandLine.Output;

/// <summary>
/// Serialises an <see cref="OutputEnvelope{T}"/> to a single line of JSON using
/// System.Text.Json source generation. Each command supplies the typed metadata for its
/// envelope shape so AOT trimming continues to work.
/// </summary>
internal static class JsonOutputWriter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static void Write<T>(
        INitroConsole console,
        OutputEnvelope<T> envelope,
        JsonTypeInfo<OutputEnvelope<T>> typeInfo)
    {
        var json = JsonSerializer.Serialize(envelope, typeInfo);
        console.Out.WriteLine(json);
    }

    /// <summary>
    /// The shared serializer options used by analytical command JSON output. Exposed so that
    /// each command's source-generated JSON context can adopt the same formatting
    /// (camelCase, indented, null-skipping).
    /// </summary>
    public static JsonSerializerOptions Options => s_options;
}
