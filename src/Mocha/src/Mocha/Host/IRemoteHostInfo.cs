using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Provides information about a remote message bus host, including machine, process, and service metadata.
/// </summary>
public interface IRemoteHostInfo
{
    /// <summary>
    /// Gets the machine/computer name.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Environment.MachineName"/>, or "unknown" if unavailable.
    /// </remarks>
    string MachineName { get; }

    /// <summary>
    /// Gets the name of the running process.
    /// </summary>
    /// <remarks>Default: <see cref="Process.GetCurrentProcess"/>.ProcessName</remarks>
    string ProcessName { get; }

    /// <summary>
    /// Gets the OS process ID.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Environment.ProcessId"/> (.NET 5+) or <see cref="Process.Id"/>.
    /// </remarks>
    int ProcessId { get; }

    /// <summary>
    /// Gets the entry assembly name.
    /// </summary>
    /// <remarks>Default: <see cref="Assembly.GetEntryAssembly"/>?.GetName().Name</remarks>
    string? AssemblyName { get; }

    /// <summary>
    /// Gets the entry assembly version.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Assembly.GetEntryAssembly"/>?.GetName().Version?.ToString()
    /// </remarks>
    string? AssemblyVersion { get; }

    /// <summary>
    /// Gets the package version.
    /// </summary>
    /// <remarks>Default: typeof(HostInfo).Assembly.GetName().Version?.ToString()</remarks>
    string? PackageVersion { get; }

    /// <summary>
    /// Gets the .NET Framework/Runtime version.
    /// </summary>
    /// <remarks>Default: <see cref="RuntimeInformation.FrameworkDescription"/></remarks>
    string FrameworkVersion { get; }

    /// <summary>
    /// Gets the operating system description.
    /// </summary>
    /// <remarks>Default: <see cref="RuntimeInformation.OSDescription"/></remarks>
    string OperatingSystemVersion { get; }

    /// <summary>
    /// Gets the environment name (e.g., Development, Staging, Production).
    /// </summary>
    /// <remarks>
    /// Default: ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variable, or "Production"
    /// if neither is set.
    /// </remarks>
    string EnvironmentName { get; }

    /// <summary>
    /// Gets the logical service name.
    /// </summary>
    /// <remarks>
    /// Default: SERVICE_NAME or OTEL_SERVICE_NAME environment variable, or entry assembly name
    /// if neither is set.
    /// </remarks>
    string? ServiceName { get; }

    /// <summary>
    /// Gets the semantic version of the service.
    /// </summary>
    /// <remarks>
    /// Default: SERVICE_VERSION environment variable, or AssemblyInformationalVersion (with build
    /// metadata stripped), or assembly version.
    /// </remarks>
    string? ServiceVersion { get; }

    /// <summary>
    /// Gets the instance ID.
    /// </summary>
    /// <remarks>Default: <see cref="Guid.NewGuid()"/>.</remarks>
    Guid InstanceId { get; }
}
