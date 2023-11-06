using System.Text.Encodings.Web;
using System.Text.Json;
using static System.Text.Json.JsonSerializerDefaults;
using static System.Text.Json.Serialization.JsonIgnoreCondition;
using static HotChocolate.Execution.Serialization.JsonNullIgnoreCondition;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// The JSON result formatter options.
/// </summary>
public struct JsonResultFormatterOptions
{
    /// <summary>
    /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
    /// should pretty print the JSON which includes:
    /// indenting nested JSON tokens, adding new lines, and adding
    /// white space between property names and values.
    /// By default, the JSON is written without any extra white space.
    /// </summary>
    public bool Indented { get; set; }

    /// <summary>
    /// Defines if null values are striped from the result.
    /// </summary>
    public JsonNullIgnoreCondition NullIgnoreCondition { get; set; }

    /// <summary>
    /// Gets or sets the encoder to use when escaping strings, or null to use the default encoder.
    /// </summary>
    public JavaScriptEncoder? Encoder { get; set; }

    internal JsonWriterOptions CreateWriterOptions()
        => new()
        {
            Indented = Indented,
            Encoder = Encoder ?? JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    internal JsonSerializerOptions CreateSerializerOptions()
        => new(Web)
        {
            WriteIndented = Indented,
            Encoder = Encoder ?? JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition =
                NullIgnoreCondition is Fields or All
                    ? WhenWritingNull
                    : default
        };
}
