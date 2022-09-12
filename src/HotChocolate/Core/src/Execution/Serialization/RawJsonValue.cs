using System;

namespace HotChocolate.Execution.Serialization;

/// <summary>
/// This helper class allows to add raw json values to response objects.
/// The JSON query result formatter will take the inner <see cref="Value"/>
/// and writes it without validation to the JSON response object.
/// </summary>
internal readonly struct RawJsonValue
{
    /// <summary>
    /// Initializes a new instance of <see cref="RawJsonValue"/>.
    /// </summary>
    /// <param name="value">
    /// The raw JSON value.
    /// </param>
    public RawJsonValue(ReadOnlyMemory<byte> value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw JSON value.
    /// </summary>
    public ReadOnlyMemory<byte> Value { get; }
}
