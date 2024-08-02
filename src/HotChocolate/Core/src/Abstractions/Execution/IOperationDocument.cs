namespace HotChocolate.Execution;

/// <summary>
/// Represents a GraphQL operation document.
/// </summary>
public interface IOperationDocument
{
    /// <summary>
    /// Writes the current document to the output stream.
    /// </summary>
    Task WriteToAsync(Stream output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the binary document representation.
    /// </summary>
    ReadOnlySpan<byte> AsSpan();

    /// <summary>
    /// Returns the binary document representation.
    /// </summary>
    byte[] ToArray();

    /// <summary>
    /// Returns the document string representation.
    /// </summary>
    string ToString();
}
