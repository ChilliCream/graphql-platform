namespace HotChocolate.Buffers;

/// <summary>
/// A helper class to marshal the underlying buffer of a <see cref="PooledArrayWriter"/>.
/// </summary>
public static class PooledArrayWriterMarshal
{
    /// <summary>
    /// Gets the underlying buffer of a <see cref="PooledArrayWriter"/>.
    /// </summary>
    /// <param name="writer">
    /// The <see cref="PooledArrayWriter"/> to get the underlying buffer from.
    /// </param>
    /// <returns>
    /// The underlying buffer of the <paramref name="writer"/>.
    /// </returns>
    public static byte[] GetUnderlyingBuffer(PooledArrayWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return writer.GetInternalBuffer();
    }
}
