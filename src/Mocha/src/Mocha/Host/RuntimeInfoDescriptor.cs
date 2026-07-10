namespace Mocha.Middlewares;

/// <summary>
/// Descriptor for configuring runtime information.
/// </summary>
public sealed class RuntimeInfoDescriptor : IRuntimeInfoDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeInfoDescriptor"/> class.
    /// </summary>
    public RuntimeInfoDescriptor()
    {
        Configuration = new RuntimeInfoConfiguration();
    }

    /// <summary>
    /// Gets the configuration object being built.
    /// </summary>
    public RuntimeInfoConfiguration Configuration { get; private set; }

    /// <inheritdoc />
    public IRuntimeInfoDescriptor RuntimeIdentifier(string runtimeIdentifier)
    {
        Configuration.RuntimeIdentifier = runtimeIdentifier;
        return this;
    }

    /// <inheritdoc />
    public IRuntimeInfoDescriptor IsServerGC(bool isServerGC)
    {
        Configuration.IsServerGC = isServerGC;
        return this;
    }

    /// <inheritdoc />
    public IRuntimeInfoDescriptor ProcessorCount(int processorCount)
    {
        Configuration.ProcessorCount = processorCount;
        return this;
    }

    /// <inheritdoc />
    public IRuntimeInfoDescriptor ProcessStartTime(DateTimeOffset processStartTime)
    {
        Configuration.ProcessStartTime = processStartTime;
        return this;
    }

    /// <inheritdoc />
    public IRuntimeInfoDescriptor IsAotCompiled(bool isAotCompiled)
    {
        Configuration.IsAotCompiled = isAotCompiled;
        return this;
    }

    /// <inheritdoc />
    public IRuntimeInfoDescriptor DebuggerAttached(bool debuggerAttached)
    {
        Configuration.DebuggerAttached = debuggerAttached;
        return this;
    }

    /// <summary>
    /// Creates the configuration object with all configured values.
    /// </summary>
    /// <returns>The configured <see cref="RuntimeInfoConfiguration"/> instance.</returns>
    public RuntimeInfoConfiguration CreateConfiguration()
    {
        return Configuration;
    }
}
