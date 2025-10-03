using System.Buffers;
using System.IO.Pipelines;

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
    /// <param name="writer">
    /// The pipe writer of the transport layer.
    /// </param>
    /// <param name="indented">
    /// Specifies if the JSON shall be indented.
    /// </param>
    void WriteTo(IBufferWriter<byte> writer, bool indented = false);
}
