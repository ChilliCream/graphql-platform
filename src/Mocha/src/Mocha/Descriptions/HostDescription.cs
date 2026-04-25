namespace Mocha;

/// <summary>
/// Describes the host application instance for diagnostic and visualization purposes.
/// </summary>
/// <param name="ServiceName">The logical service name, or <c>null</c> if not configured.</param>
/// <param name="AssemblyName">The entry assembly name, or <c>null</c> if not available.</param>
/// <param name="InstanceId">The unique identifier for this host instance.</param>
internal sealed record HostDescription(string? ServiceName, string? AssemblyName, string InstanceId);
