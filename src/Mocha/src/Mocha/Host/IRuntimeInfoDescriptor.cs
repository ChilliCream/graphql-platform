using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Mocha.Middlewares;

/// <summary>
/// Descriptor interface for configuring runtime information.
/// </summary>
public interface IRuntimeInfoDescriptor
{
    /// <summary>
    /// Sets the .NET Runtime Identifier (e.g., linux-x64, win-x64).
    /// </summary>
    /// <param name="runtimeIdentifier">
    /// The runtime identifier. If not specified, defaults to
    /// <see cref="RuntimeInformation.RuntimeIdentifier"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="RuntimeInformation.RuntimeIdentifier"/></remarks>
    IRuntimeInfoDescriptor RuntimeIdentifier(string runtimeIdentifier);

    /// <summary>
    /// Sets whether server GC is enabled.
    /// </summary>
    /// <param name="isServerGC">
    /// True if server GC is enabled. If not specified, defaults to
    /// <see cref="GCSettings.IsServerGC"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="GCSettings.IsServerGC"/></remarks>
    IRuntimeInfoDescriptor IsServerGC(bool isServerGC);

    /// <summary>
    /// Sets the number of processors available.
    /// </summary>
    /// <param name="processorCount">
    /// The processor count. If not specified, defaults to <see cref="Environment.ProcessorCount"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="Environment.ProcessorCount"/></remarks>
    IRuntimeInfoDescriptor ProcessorCount(int processorCount);

    /// <summary>
    /// Sets when the process started.
    /// </summary>
    /// <param name="processStartTime">
    /// The process start time. If not specified, defaults to the current process start time.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: <see cref="Process.GetCurrentProcess"/>.StartTime, or null if unavailable.
    /// </remarks>
    IRuntimeInfoDescriptor ProcessStartTime(DateTimeOffset processStartTime);

    /// <summary>
    /// Sets whether running in AOT compiled mode (.NET 8+).
    /// </summary>
    /// <param name="isAotCompiled">
    /// True if AOT compiled. If not specified, defaults to checking runtime features.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>
    /// Default: !RuntimeFeature.IsDynamicCodeSupported (.NET 8+), or null in older versions.
    /// </remarks>
    IRuntimeInfoDescriptor IsAotCompiled(bool isAotCompiled);

    /// <summary>
    /// Sets whether a debugger is attached.
    /// </summary>
    /// <param name="debuggerAttached">
    /// True if debugger is attached. If not specified, defaults to
    /// <see cref="Debugger.IsAttached"/>.
    /// </param>
    /// <returns>The descriptor instance for method chaining.</returns>
    /// <remarks>Default: <see cref="Debugger.IsAttached"/></remarks>
    IRuntimeInfoDescriptor DebuggerAttached(bool debuggerAttached);
}
