using System.Reflection;

namespace Mocha.Middlewares;

/// <summary>
/// Descriptor for configuring host information.
/// </summary>
public sealed class HostInfoDescriptor : IHostInfoDescriptor
{
    private readonly RuntimeInfoDescriptor _runtimeDescriptor = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HostInfoDescriptor"/> class.
    /// </summary>
    public HostInfoDescriptor()
    {
        Configuration = new HostInfoConfiguration();
    }

    /// <summary>
    /// Gets the configuration object being built.
    /// </summary>
    public HostInfoConfiguration Configuration { get; private set; }

    /// <inheritdoc />
    public IHostInfoDescriptor MachineName(string machineName)
    {
        Configuration.MachineName = machineName;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor ProcessName(string processName)
    {
        Configuration.ProcessName = processName;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor ProcessId(int processId)
    {
        Configuration.ProcessId = processId;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor Assembly(Assembly assembly)
    {
        Configuration.Assembly = assembly;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor AssemblyName(string assemblyName)
    {
        Configuration.AssemblyName = assemblyName;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor AssemblyVersion(string assemblyVersion)
    {
        Configuration.AssemblyVersion = assemblyVersion;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor PackageVersion(string packageVersion)
    {
        Configuration.PackageVersion = packageVersion;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor FrameworkVersion(string frameworkVersion)
    {
        Configuration.FrameworkVersion = frameworkVersion;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor OperatingSystemVersion(string operatingSystemVersion)
    {
        Configuration.OperatingSystemVersion = operatingSystemVersion;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor EnvironmentName(string environmentName)
    {
        Configuration.EnvironmentName = environmentName;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor ServiceName(string serviceName)
    {
        Configuration.ServiceName = serviceName;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor ServiceVersion(string serviceVersion)
    {
        Configuration.ServiceVersion = serviceVersion;
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor RuntimeInfo(Action<IRuntimeInfoDescriptor> configure)
    {
        configure(_runtimeDescriptor);
        return this;
    }

    /// <inheritdoc />
    public IHostInfoDescriptor InstanceId(Guid instanceId)
    {
        Configuration.InstanceId = instanceId;
        return this;
    }

    /// <summary>
    /// Creates the configuration object with all configured values.
    /// </summary>
    /// <returns>The configured <see cref="HostInfoConfiguration"/> instance.</returns>
    public HostInfoConfiguration CreateConfiguration()
    {
        Configuration.RuntimeInfo = _runtimeDescriptor.CreateConfiguration();
        return Configuration;
    }
}
