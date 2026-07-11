using System.Text.Json;

namespace HotChocolate.Buffers;

/// <summary>
/// A <see cref="JsonDocument"/> that owns the memory it was created with.
/// This helper holds both the <see cref="JsonDocument"/> and the memory the document was parsed from.
/// </summary>
public sealed class JsonDocumentOwner : IDisposable
{
    private readonly IDisposable? _memoryOwner;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonDocumentOwner"/>.
    /// </summary>
    /// <param name="document">
    /// The <see cref="JsonDocument"/> to own.
    /// </param>
    public JsonDocumentOwner(JsonDocument document)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(document);
#else
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }
#endif

        Document = document;
    }
    /// <summary>
    /// Initializes a new instance of <see cref="JsonDocumentOwner"/>.
    /// </summary>
    /// <param name="document">
    /// The <see cref="JsonDocument"/> to own.
    /// </param>
    /// <param name="memoryOwner">
    /// The memory that was used to create the <paramref name="document"/>.
    /// </param>
    public JsonDocumentOwner(JsonDocument document, IDisposable memoryOwner)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(memoryOwner);
#else
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (memoryOwner is null)
        {
            throw new ArgumentNullException(nameof(memoryOwner));
        }
#endif

        Document = document;
        _memoryOwner = memoryOwner;
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
            _memoryOwner?.Dispose();

            _disposed = true;
        }
    }
}
