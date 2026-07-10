namespace Mocha.Middlewares;

/// <summary>
/// Represents host information about the current process and environment.
/// </summary>
public sealed class HostInfo : IHostInfo
{
    /// <inheritdoc />
    public required string MachineName { get; init; }

    /// <inheritdoc />
    public required string ProcessName { get; init; }

    /// <inheritdoc />
    public required int ProcessId { get; init; }

    /// <inheritdoc />
    public required string? AssemblyName { get; init; }

    /// <inheritdoc />
    public required string? AssemblyVersion { get; init; }

    /// <inheritdoc />
    public required string? PackageVersion { get; init; }

    /// <inheritdoc />
    public required string FrameworkVersion { get; init; }

    /// <inheritdoc />
    public required string OperatingSystemVersion { get; init; }

    /// <inheritdoc />
    public required string EnvironmentName { get; init; }

    /// <inheritdoc />
    public required string? ServiceName { get; init; }

    /// <inheritdoc />
    public required string? ServiceVersion { get; init; }

    /// <inheritdoc />
    public required IRuntimeInfo RuntimeInfo { get; init; }

    /// <inheritdoc />
    public required Guid InstanceId { get; init; }
}
