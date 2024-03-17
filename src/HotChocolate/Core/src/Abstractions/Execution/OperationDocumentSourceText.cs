using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a GraphQL operation document source texts that needs parsing before it can be executed.
/// </summary>
/// <param name="sourceText"></param>
public sealed class OperationDocumentSourceText(string sourceText) : IOperationDocument
{
    /// <summary>
    /// Gets the GraphQL operation document source text. 
    /// </summary>
    public string SourceText { get; } = sourceText ?? throw new ArgumentNullException(nameof(sourceText));

    /// <summary>
    /// Writes the current document to the output stream.
    /// </summary>
    /// <param name="output">
    /// The output stream to which the document is written.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="output"/> is <c>null</c>.
    /// </exception>
    public async Task WriteToAsync(Stream output, CancellationToken cancellationToken = default)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }
        
        var buffer = Encoding.UTF8.GetBytes(SourceText);
        await output.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the binary document representation.
    /// </summary>
    /// <returns>
    /// Returns the binary document representation.
    /// </returns>
    public ReadOnlySpan<byte> AsSpan()
        => Encoding.UTF8.GetBytes(SourceText);

    public byte[] ToArray()
        => Encoding.UTF8.GetBytes(SourceText);

    /// <summary>
    /// Returns the document string representation.
    /// </summary>
    /// <returns>
    /// Returns the document string representation.
    /// </returns>
    public override string ToString() => SourceText;
}
