using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Descriptor interface for configuring host information.
/// </summary>
public interface IHostInfoDescriptor
{
    /// <summary>
    /// Sets the machine/computer name.
    /// </summary>
    /// <param name="machineName">
    /// The machine name. If not specified, defaults to <see cref="Environment.MachineName"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: <see cref="Environment.MachineName"/>, or "unknown" if unavailable.
    /// </remarks>
    IHostInfoDescriptor MachineName(string machineName);

    /// <summary>
    /// Sets the name of the running process.
    /// </summary>
    /// <param name="processName">
    /// The process name. If not specified, defaults to <see cref="Process.ProcessName"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="Process.GetCurrentProcess"/>.ProcessName</remarks>
    IHostInfoDescriptor ProcessName(string processName);

    /// <summary>
    /// Sets the OS process ID.
    /// </summary>
    /// <param name="processId">
    /// The process ID. If not specified, defaults to <see cref="Environment.ProcessId"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: <see cref="Environment.ProcessId"/> (.NET 5+) or <see cref="Process.Id"/>.
    /// </remarks>
    IHostInfoDescriptor ProcessId(int processId);

    /// <summary>
    /// Sets the entry assembly.
    /// </summary>
    /// <param name="assembly">
    /// The assembly. If not specified, defaults to <see cref="Assembly.GetEntryAssembly"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="Assembly.GetEntryAssembly"/>.</remarks>
    IHostInfoDescriptor Assembly(Assembly assembly);

    /// <summary>
    /// Sets the entry assembly name.
    /// </summary>
    /// <param name="assemblyName">
    /// The assembly name. If not specified, defaults to the entry assembly's name.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="Assembly.GetEntryAssembly"/>?.GetName().Name</remarks>
    IHostInfoDescriptor AssemblyName(string assemblyName);

    /// <summary>
    /// Sets the entry assembly version.
    /// </summary>
    /// <param name="assemblyVersion">
    /// The assembly version. If not specified, defaults to the entry assembly's version.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: <see cref="Assembly.GetEntryAssembly"/>?.GetName().Version?.ToString()
    /// </remarks>
    IHostInfoDescriptor AssemblyVersion(string assemblyVersion);

    /// <summary>
    /// Sets the package version.
    /// </summary>
    /// <param name="packageVersion">
    /// The package version. If not specified, defaults to the HostInfo assembly version.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: typeof(HostInfo).Assembly.GetName().Version?.ToString()</remarks>
    IHostInfoDescriptor PackageVersion(string packageVersion);

    /// <summary>
    /// Sets the .NET Framework/Runtime version.
    /// </summary>
    /// <param name="frameworkVersion">
    /// The framework version. If not specified, defaults to
    /// <see cref="RuntimeInformation.FrameworkDescription"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="RuntimeInformation.FrameworkDescription"/></remarks>
    IHostInfoDescriptor FrameworkVersion(string frameworkVersion);

    /// <summary>
    /// Sets the operating system description.
    /// </summary>
    /// <param name="operatingSystemVersion">
    /// The OS version. If not specified, defaults to
    /// <see cref="RuntimeInformation.OSDescription"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="RuntimeInformation.OSDescription"/></remarks>
    IHostInfoDescriptor OperatingSystemVersion(string operatingSystemVersion);

    /// <summary>
    /// Sets the environment name (e.g., Development, Staging, Production).
    /// </summary>
    /// <param name="environmentName">
    /// The environment name. If not specified, defaults to environment variable.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variable, or "Production" if
    /// neither is set.
    /// </remarks>
    IHostInfoDescriptor EnvironmentName(string environmentName);

    /// <summary>
    /// Sets the logical service name.
    /// </summary>
    /// <param name="serviceName">
    /// The service name. If not specified, defaults to environment variable or assembly name.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: SERVICE_NAME or OTEL_SERVICE_NAME environment variable, or entry assembly name if
    /// neither is set.
    /// </remarks>
    IHostInfoDescriptor ServiceName(string serviceName);

    /// <summary>
    /// Sets the semantic version of the service.
    /// </summary>
    /// <param name="serviceVersion">
    /// The service version. If not specified, defaults to environment variable or assembly version.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: SERVICE_VERSION environment variable, or AssemblyInformationalVersion (with build
    /// metadata stripped), or assembly version.
    /// </remarks>
    IHostInfoDescriptor ServiceVersion(string serviceVersion);

    /// <summary>
    /// Configures runtime information.
    /// </summary>
    /// <param name="configure">The configuration action for runtime info.</param>
    /// <returns>The descriptor instance for method chaining.</returns>
    IHostInfoDescriptor RuntimeInfo(Action<IRuntimeInfoDescriptor> configure);

    /// <summary>
    /// Sets the instance ID.
    /// </summary>
    /// <param name="instanceId">
    /// The instance ID. If not specified, defaults to a new version 7 GUID.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="Guid.NewGuid()"/>.</remarks>
    IHostInfoDescriptor InstanceId(Guid instanceId);
}
