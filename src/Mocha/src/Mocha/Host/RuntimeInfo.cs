namespace Mocha.Middlewares;

/// <summary>
/// Represents runtime information about the current process.
/// </summary>
public sealed class RuntimeInfo : IRuntimeInfo
{
    /// <inheritdoc />
    public string? RuntimeIdentifier { get; set; }

    /// <inheritdoc />
    public bool IsServerGC { get; set; }

    /// <inheritdoc />
    public int ProcessorCount { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ProcessStartTime { get; set; }

    /// <inheritdoc />
    public bool? IsAotCompiled { get; set; }

    /// <inheritdoc />
    public bool DebuggerAttached { get; set; }
}
