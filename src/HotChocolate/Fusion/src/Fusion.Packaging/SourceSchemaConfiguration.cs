using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents a Hot Chocolate Fusion source schema configuration.
/// </summary>
public sealed class SourceSchemaConfiguration : IDisposable
{
    private readonly Func<CancellationToken, Task<Stream>> _openReadSchema;
    private readonly Func<CancellationToken, Task<Stream?>> _tryOpenReadSchemaExtensions;
    private bool _disposed;

    internal SourceSchemaConfiguration(
        Func<CancellationToken, Task<Stream>> openReadSchema,
        Func<CancellationToken, Task<Stream?>> tryOpenReadSchemaExtensions,
        JsonDocument settings)
    {
        ArgumentNullException.ThrowIfNull(openReadSchema);
        ArgumentNullException.ThrowIfNull(tryOpenReadSchemaExtensions);
        ArgumentNullException.ThrowIfNull(settings);

        _openReadSchema = openReadSchema;
        _tryOpenReadSchemaExtensions = tryOpenReadSchemaExtensions;
        Settings = settings;
    }

    /// <summary>
    /// Opens the Hot Chocolate Fusion source schema for reading.
    /// </summary>
    public Task<Stream> OpenReadSchemaAsync(CancellationToken cancellationToken = default)
        => _openReadSchema(cancellationToken);

    /// <summary>
    /// Tries to open the Hot Chocolate Fusion source schema extensions for reading.
    /// Returns null if no extensions are stored alongside the source schema.
    /// </summary>
    public Task<Stream?> TryOpenReadSchemaExtensionsAsync(CancellationToken cancellationToken = default)
        => _tryOpenReadSchemaExtensions(cancellationToken);

    /// <summary>
    /// Gets the settings of the source schema configuration.
    /// </summary>
    public JsonDocument Settings { get; }

    /// <summary>
    /// Disposes the source schema configuration.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Settings.Dispose();
            _disposed = true;
        }
    }
}
