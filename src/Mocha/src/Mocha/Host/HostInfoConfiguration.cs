using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Configuration class for host information. Properties are optional and will use defaults if not
/// specified.
/// </summary>
public sealed class HostInfoConfiguration
{
    /// <summary>
    /// Gets or sets the machine/computer name.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Environment.MachineName"/>, or "unknown" if unavailable.
    /// </remarks>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets the name of the running process.
    /// </summary>
    /// <remarks>Default: <see cref="Process.GetCurrentProcess"/>.ProcessName</remarks>
    public string? ProcessName { get; set; }

    /// <summary>
    /// Gets or sets the OS process ID.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Environment.ProcessId"/> (.NET 5+) or <see cref="Process.Id"/>.
    /// </remarks>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the entry assembly.
    /// </summary>
    /// <remarks>Default: <see cref="Assembly.GetEntryAssembly"/>.</remarks>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// Gets or sets the entry assembly name.
    /// </summary>
    /// <remarks>Default: <see cref="Assembly.GetEntryAssembly"/>?.GetName().Name</remarks>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Gets or sets the entry assembly version.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Assembly.GetEntryAssembly"/>?.GetName().Version?.ToString()
    /// </remarks>
    public string? AssemblyVersion { get; set; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    /// <remarks>Default: typeof(HostInfo).Assembly.GetName().Version?.ToString()</remarks>
    public string? PackageVersion { get; set; }

    /// <summary>
    /// Gets or sets the .NET Framework/Runtime version.
    /// </summary>
    /// <remarks>Default: <see cref="RuntimeInformation.FrameworkDescription"/></remarks>
    public string? FrameworkVersion { get; set; }

    /// <summary>
    /// Gets or sets the operating system description.
    /// </summary>
    /// <remarks>Default: <see cref="RuntimeInformation.OSDescription"/></remarks>
    public string? OperatingSystemVersion { get; set; }

    /// <summary>
    /// Gets or sets the environment name (e.g., Development, Staging, Production).
    /// </summary>
    /// <remarks>
    /// Default: ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variable, or "Production"
    /// if neither is set.
    /// </remarks>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// Gets or sets the logical service name.
    /// </summary>
    /// <remarks>
    /// Default: SERVICE_NAME or OTEL_SERVICE_NAME environment variable, or entry assembly name
    /// if neither is set.
    /// </remarks>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the semantic version of the service.
    /// </summary>
    /// <remarks>
    /// Default: SERVICE_VERSION environment variable, or AssemblyInformationalVersion (with build
    /// metadata stripped), or assembly version.
    /// </remarks>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the runtime information configuration.
    /// </summary>
    /// <remarks>
    /// If not specified, a new <see cref="RuntimeInfoConfiguration"/> will be created with default
    /// values.
    /// </remarks>
    public RuntimeInfoConfiguration? RuntimeInfo { get; set; }

    /// <summary>
    /// Gets or sets the instance ID.
    /// </summary>
    /// <remarks>Default: <see cref="Guid.NewGuid()"/>.</remarks>
    public Guid? InstanceId { get; set; }
}
