using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Execution;

/// <summary>
/// Represents an already parsed GraphQL operation document.
/// </summary>
/// <param name="document">
/// The parsed GraphQL operation document.
/// </param>
public sealed class OperationDocument(DocumentNode document) : IOperationDocument
{
    /// <summary>
    /// Gets the parsed GraphQL operation document.
    /// </summary>
    public DocumentNode Document { get; } = document ?? throw new ArgumentNullException(nameof(document));
    
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
        
        await Document.PrintToAsync(output, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns the binary document representation.
    /// </summary>
    /// <returns>
    /// Returns the binary document representation.
    /// </returns>
    public ReadOnlySpan<byte> AsSpan()
        => Encoding.UTF8.GetBytes(Document.Print(false));

    /// <summary>
    /// Returns the binary document representation.
    /// </summary>
    /// <returns>
    /// Returns the binary document representation.
    /// </returns>
    public byte[] ToArray()
        => Encoding.UTF8.GetBytes(Document.Print(false));

    /// <summary>
    /// Returns the document string representation.
    /// </summary>
    /// <returns>
    /// Returns the document string representation.
    /// </returns>
    public override string ToString()
        => Document.Print();
}
