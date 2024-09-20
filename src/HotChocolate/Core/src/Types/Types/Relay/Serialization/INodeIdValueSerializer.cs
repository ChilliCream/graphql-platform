#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Relay;

/// <summary>
/// The ID serializer is used to parse and format the value part if a node id.
/// </summary>
public interface INodeIdValueSerializer
{
    /// <summary>
    /// Defines if the serializer supports formatting and parsing the specified type.
    /// </summary>
    /// <param name="type">
    /// The type that shall be checked.
    /// </param>
    /// <returns>
    /// Returns true if the serializer supports the specified type.
    /// </returns>
    bool IsSupported(Type type);

    /// <summary>
    /// Formats the node id value into a byte buffer.
    /// </summary>
    /// <param name="buffer">
    /// The buffer to write the formatted value into.
    /// </param>
    /// <param name="value">
    /// The value to format.
    /// </param>
    /// <param name="written">
    /// The number of bytes written to the buffer.
    /// </param>
    NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written);

    /// <summary>
    /// Parses the node id value from a byte buffer.
    /// </summary>
    /// <param name="buffer">
    /// The byte buffer that contains the formatted id value.
    /// </param>
    /// <param name="value">
    /// The parsed value.
    /// </param>
    /// <returns>
    /// Returns true if the value could be parsed.
    /// </returns>
    bool TryParse(ReadOnlySpan<byte> buffer, [NotNullWhen(true)] out object? value);
}
