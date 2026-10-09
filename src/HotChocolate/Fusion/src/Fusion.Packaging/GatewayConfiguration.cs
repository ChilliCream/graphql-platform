using System.Text.Json;

namespace HotChocolate.Fusion.Packaging;

/// <summary>
/// Represents a Hot Chocolate Fusion gateway configuration.
/// </summary>
public sealed class GatewayConfiguration : IDisposable
{
    private readonly Func<CancellationToken, Task<Stream>> _openReadSchema;
    private bool _disposed;

    internal GatewayConfiguration(
        Func<CancellationToken, Task<Stream>> openReadSchema,
        JsonDocument settings,
        Version version,
        string? planningFingerprint)
    {
        ArgumentNullException.ThrowIfNull(openReadSchema);
        ArgumentNullException.ThrowIfNull(settings);

        _openReadSchema = openReadSchema;
        Settings = settings;
        Version = version;
        PlanningFingerprint = planningFingerprint;
    }

    /// <summary>
    /// Gets the version of the gateway configuration.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets an opaque fingerprint of the configuration inputs relevant to operation planning,
    /// or null if the archive does not contain one.
    /// </summary>
    public string? PlanningFingerprint { get; }

    /// <summary>
    /// Opens the Hot Chocolate Fusion execution schema for reading.
    /// </summary>
    public Task<Stream> OpenReadSchemaAsync(CancellationToken cancellationToken = default)
        => _openReadSchema(cancellationToken);

    /// <summary>
    /// Gets the settings of the gateway configuration.
    /// </summary>
    public JsonDocument Settings { get; }

    /// <summary>
    /// Disposes the gateway configuration.
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
