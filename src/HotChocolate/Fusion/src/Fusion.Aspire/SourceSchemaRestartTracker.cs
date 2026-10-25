using System.Collections.Concurrent;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Tracks ready signals per resource so that the initial start of a resource can be
/// distinguished from subsequent restarts.
/// </summary>
internal sealed class SourceSchemaRestartTracker
{
    private readonly ConcurrentDictionary<string, byte> _startedResources = new(StringComparer.Ordinal);

    /// <summary>
    /// Records a ready signal for the resource. Returns false for the first signal per
    /// resource, which represents the initial start, and true for every later signal.
    /// </summary>
    public bool IsRestart(string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(resourceName);
        return !_startedResources.TryAdd(resourceName, 0);
    }
}
