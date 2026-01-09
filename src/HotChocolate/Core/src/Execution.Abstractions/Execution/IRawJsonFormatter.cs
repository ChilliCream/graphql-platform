using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a low level abstraction that allows the implementor to opt out of the
/// standard JSON serialization to emit the JSON directly into the pipe writer
/// of the transport layer.
/// </summary>
public interface IRawJsonFormatter
{
    /// <summary>
    /// Writes the JSON data into the <paramref name="writer"/>.
    /// </summary>
    /// <param name="result">
    /// The result that shall be serialized.
    /// </param>
    /// <param name="writer">
    /// The buffer writer of the transport layer.
    /// </param>
    /// <param name="options">
    /// The JSON writer options.
    /// </param>
    void WriteTo(
        OperationResult result,
        IBufferWriter<byte> writer,
        JsonWriterOptions options = default);
}
