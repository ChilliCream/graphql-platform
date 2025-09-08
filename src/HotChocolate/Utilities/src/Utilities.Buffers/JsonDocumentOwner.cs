using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Buffers;

/// <summary>
/// A <see cref="JsonDocument"/> that owns the memory it was created with.
/// This helper holds both the <see cref="JsonDocument"/> and the memory the document was parsed from.
/// </summary>
public sealed class JsonDocumentOwner : IDisposable
{
    private readonly IMemoryOwner<byte> _memory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonDocumentOwner"/>.
    /// </summary>
    /// <param name="document">
    /// The <see cref="JsonDocument"/> to own.
    /// </param>
    /// <param name="memory">
    /// The memory that was used to create the <paramref name="document"/>.
    /// </param>
    public JsonDocumentOwner(JsonDocument document, IMemoryOwner<byte> memory)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(memory);
#else
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (memory is null)
        {
            throw new ArgumentNullException(nameof(memory));
        }
#endif

        Document = document;
        _memory = memory;
    }

    /// <summary>
    /// Gets the <see cref="JsonDocument"/> that is owned by this instance.
    /// </summary>
    public JsonDocument Document { get; }

    /// <summary>
    /// Disposes the <see cref="JsonDocument"/> and the memory the document was parsed from.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Document.Dispose();
            _memory.Dispose();

            _disposed = true;
        }
    }
}
