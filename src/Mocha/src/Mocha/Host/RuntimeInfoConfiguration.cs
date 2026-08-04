using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Configuration class for runtime information. Properties are optional and will use defaults if
/// not specified.
/// </summary>
public class RuntimeInfoConfiguration
{
    /// <summary>
    /// Gets or sets the .NET Runtime Identifier (e.g., linux-x64, win-x64).
    /// </summary>
    /// <remarks>Default: <see cref="RuntimeInformation.RuntimeIdentifier"/></remarks>
    public string? RuntimeIdentifier { get; set; }

    /// <summary>
    /// Gets or sets whether server GC is enabled.
    /// </summary>
    /// <remarks>Default: <see cref="GCSettings.IsServerGC"/></remarks>
    public bool? IsServerGC { get; set; }

    /// <summary>
    /// Gets or sets the number of processors available.
    /// </summary>
    /// <remarks>Default: <see cref="Environment.ProcessorCount"/></remarks>
    public int? ProcessorCount { get; set; }

    /// <summary>
    /// Gets or sets when the process started.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Process.GetCurrentProcess"/>.StartTime, or null if unavailable.
    /// </remarks>
    public DateTimeOffset? ProcessStartTime { get; set; }

    /// <summary>
    /// Gets or sets whether running in AOT compiled mode (.NET 8+).
    /// </summary>
    /// <remarks>
    /// Default: !RuntimeFeature.IsDynamicCodeSupported (.NET 8+), or null in older versions.
    /// </remarks>
    public bool? IsAotCompiled { get; set; }

    /// <summary>
    /// Gets or sets whether a debugger is attached.
    /// </summary>
    /// <remarks>Default: <see cref="Debugger.IsAttached"/></remarks>
    public bool? DebuggerAttached { get; set; }
}
