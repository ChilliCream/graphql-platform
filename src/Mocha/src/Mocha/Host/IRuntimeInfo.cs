using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Interface representing runtime information about the current process.
/// </summary>
public interface IRuntimeInfo
{
    /// <summary>
    /// Gets the .NET Runtime Identifier (e.g., linux-x64, win-x64).
    /// </summary>
    /// <remarks>Default: <see cref="RuntimeInformation.RuntimeIdentifier"/></remarks>
    string? RuntimeIdentifier { get; }

    /// <summary>
    /// Gets whether server GC is enabled.
    /// </summary>
    /// <remarks>Default: <see cref="GCSettings.IsServerGC"/></remarks>
    bool IsServerGC { get; }

    /// <summary>
    /// Gets the number of processors available.
    /// </summary>
    /// <remarks>Default: <see cref="Environment.ProcessorCount"/></remarks>
    int ProcessorCount { get; }

    /// <summary>
    /// Gets when the process started.
    /// </summary>
    /// <remarks>
    /// Default: <see cref="Process.GetCurrentProcess"/>.StartTime, or null if unavailable.
    /// </remarks>
    DateTimeOffset? ProcessStartTime { get; }

    /// <summary>
    /// Gets whether running in AOT compiled mode (.NET 8+).
    /// </summary>
    /// <remarks>
    /// Default: !RuntimeFeature.IsDynamicCodeSupported (.NET 8+), or null in older versions.
    /// </remarks>
    bool? IsAotCompiled { get; }

    /// <summary>
    /// Gets whether a debugger is attached.
    /// </summary>
    /// <remarks>Default: <see cref="Debugger.IsAttached"/></remarks>
    bool DebuggerAttached { get; }
}
