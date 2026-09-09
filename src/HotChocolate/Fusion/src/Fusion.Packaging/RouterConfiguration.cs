using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents a Hot Chocolate Fusion router configuration.
/// The persisted archive entries keep the legacy gateway names
/// (gateway/{version}/gateway.graphqls and gateway/{version}/gateway-settings.json).
/// </summary>
public sealed class RouterConfiguration : IDisposable
{
    private readonly Func<CancellationToken, Task<Stream>> _openReadSchema;
    private bool _disposed;

    internal RouterConfiguration(
        Func<CancellationToken, Task<Stream>> openReadSchema,
        JsonDocument settings,
        Version version)
    {
        ArgumentNullException.ThrowIfNull(openReadSchema);
        ArgumentNullException.ThrowIfNull(settings);

        _openReadSchema = openReadSchema;
        Settings = settings;
        Version = version;
    }

    /// <summary>
    /// Gets the version of the router configuration.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Opens the Hot Chocolate Fusion execution schema for reading.
    /// </summary>
    public Task<Stream> OpenReadSchemaAsync(CancellationToken cancellationToken = default)
        => _openReadSchema(cancellationToken);

    /// <summary>
    /// Gets the settings of the router configuration.
    /// </summary>
    public JsonDocument Settings { get; }

    /// <summary>
    /// Disposes the router configuration.
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
