using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Factory class for creating <see cref="HostInfo"/> and <see cref="RuntimeInfo"/> instances from
/// configuration.
/// </summary>
internal static class HostInfoFactory
{
    /// <summary>
    /// Creates a <see cref="RuntimeInfo"/> instance from the provided configuration, using defaults
    /// for any unspecified values.
    /// </summary>
    /// <param name="configuration">
    /// The runtime information configuration. If null, all values will use defaults.
    /// </param>
    /// <returns>
    /// A new <see cref="RuntimeInfo"/> instance with configured or default values.
    /// </returns>
    public static RuntimeInfo From(RuntimeInfoConfiguration configuration)
    {
        using var process = Process.GetCurrentProcess();

        return new RuntimeInfo
        {
            RuntimeIdentifier = configuration.RuntimeIdentifier ?? RuntimeInformation.RuntimeIdentifier,
            IsServerGC = configuration.IsServerGC ?? GCSettings.IsServerGC,
            ProcessorCount = configuration.ProcessorCount ?? Environment.ProcessorCount,
            ProcessStartTime = configuration.ProcessStartTime ?? GetProcessStartTime(process),
            IsAotCompiled = configuration.IsAotCompiled ?? GetIsAotCompiled(),
            DebuggerAttached = configuration.DebuggerAttached ?? Debugger.IsAttached
        };
    }

    /// <summary>
    /// Creates a <see cref="HostInfo"/> instance from the provided configuration, using defaults
    /// for any unspecified values.
    /// </summary>
    /// <param name="configuration">
    /// The host information configuration. If null, all values will use defaults.
    /// </param>
    /// <returns>A new <see cref="HostInfo"/> instance with configured or default values.</returns>
    public static HostInfo From(HostInfoConfiguration configuration)
    {
        var entryAssembly = configuration.Assembly ?? Assembly.GetEntryAssembly();
        var process = Process.GetCurrentProcess();

        return new HostInfo
        {
            MachineName = configuration.MachineName ?? GetMachineName(),
            ProcessName = configuration.ProcessName ?? process.ProcessName,
            ProcessId = configuration.ProcessId ?? Environment.ProcessId,
            AssemblyName = configuration.AssemblyName ?? entryAssembly?.GetName().Name,
            AssemblyVersion = configuration.AssemblyVersion ?? entryAssembly?.GetName().Version?.ToString(),
            PackageVersion = configuration.PackageVersion ?? typeof(HostInfo).Assembly.GetName().Version?.ToString(), // TODO we need to stamp in the version here
            FrameworkVersion = configuration.FrameworkVersion ?? RuntimeInformation.FrameworkDescription,
            OperatingSystemVersion = configuration.OperatingSystemVersion ?? RuntimeInformation.OSDescription,
            EnvironmentName = configuration.EnvironmentName ?? GetEnvironmentName(),
            ServiceName = configuration.ServiceName ?? GetServiceName(entryAssembly),
            ServiceVersion = configuration.ServiceVersion ?? GetServiceVersion(entryAssembly),

            InstanceId = configuration.InstanceId ?? Guid.NewGuid(),

            RuntimeInfo = From(configuration.RuntimeInfo ?? new RuntimeInfoConfiguration())
        };
    }

    /// <summary>
    /// Gets the machine name, returning "unknown" if unavailable.
    /// </summary>
    /// <returns>The machine name, or "unknown" if an error occurs.</returns>
    private static string GetMachineName()
    {
        try
        {
            return Environment.MachineName;
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Gets the environment name from environment variables, defaulting to "Production".
    /// </summary>
    /// <returns>
    /// The environment name from ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT, or "Production" if
    /// neither is set.
    /// </returns>
    private static string GetEnvironmentName()
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
    }

    /// <summary>
    /// Gets the service name from environment variables or assembly name.
    /// Priority: SERVICE_NAME > OTEL_SERVICE_NAME > assembly name.
    /// </summary>
    /// <param name="entryAssembly">The entry assembly to use as fallback.</param>
    /// <returns>The service name, or null if unavailable.</returns>
    private static string? GetServiceName(Assembly? entryAssembly)
    {
        // Priority: env var > OTEL service name > assembly name
        return Environment.GetEnvironmentVariable("SERVICE_NAME")
            ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? entryAssembly?.GetName().Name;
    }

    /// <summary>
    /// Gets the service version from environment variables or assembly attributes.
    /// Priority: SERVICE_VERSION > AssemblyInformationalVersion (with build metadata stripped) >
    /// assembly version.
    /// </summary>
    /// <param name="entryAssembly">The entry assembly to extract version information from.</param>
    /// <returns>The service version, or null if unavailable.</returns>
    private static string? GetServiceVersion(Assembly? entryAssembly)
    {
        // Priority: env var > informational version > assembly version
        var envVersion = Environment.GetEnvironmentVariable("SERVICE_VERSION");
        if (!string.IsNullOrEmpty(envVersion))
        {
            return envVersion;
        }

        if (entryAssembly is null)
        {
            return null;
        }

        try
        {
            var infoVersion = entryAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            // Strip build metadata (e.g., "1.0.0+abc123" -> "1.0.0")
            if (!string.IsNullOrEmpty(infoVersion))
            {
                var plusIndex = infoVersion.IndexOf('+');

                return plusIndex > 0 ? infoVersion[..plusIndex] : infoVersion;
            }

            return entryAssembly.GetName().Version?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the process start time from the specified process.
    /// </summary>
    /// <param name="process">The process to get the start time from.</param>
    /// <returns>The process start time, or null if unavailable.</returns>
    private static DateTimeOffset? GetProcessStartTime(Process process)
    {
        try
        {
            return new DateTimeOffset(process.StartTime);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if the application is running in AOT compiled mode (.NET 8+).
    /// </summary>
    /// <returns>
    /// True if AOT compiled, false if not, or null if cannot be determined (older .NET versions).
    /// </returns>
    private static bool? GetIsAotCompiled()
    {
        try
        {
#if NET8_0_OR_GREATER
            return !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
#else
            return null; // Cannot determine in older .NET versions
#endif
        }
        catch
        {
            return null;
        }
    }
}
