using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

// TODO: Think about this again
/// <summary>
/// Represents a low level abstraction that allows the implementor to opt out of the
/// standard JSON serialization to emit the JSON directly into the pipe writer
/// of the transport layer.
/// </summary>
public interface IRawJsonFormatter
{
    /// <summary>
    /// Writes the JSON data into the <paramref name="jsonWriter"/>.
    /// </summary>
    /// <param name="jsonWriter">
    /// The JSON writer.
    /// </param>
    void WriteDataTo(JsonWriter jsonWriter);
}
