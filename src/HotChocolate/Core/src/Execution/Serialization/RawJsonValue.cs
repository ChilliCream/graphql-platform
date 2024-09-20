namespace HotChocolate.Execution.Serialization;

/// <summary>
/// This helper class allows to add raw json values to response objects.
/// The JSON query result formatter will take the inner <see cref="Value"/>
/// and writes it without validation to the JSON response object.
/// </summary>
/// <remarks>
/// <para>
/// The downside of this helper is that we bind it explicitly to JSON.
/// If there were alternative query formatter that use different formats we would get
/// into trouble with this.
/// </para>
/// <para>This is also the reason for keeping this internal.</para>
/// </remarks>
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
