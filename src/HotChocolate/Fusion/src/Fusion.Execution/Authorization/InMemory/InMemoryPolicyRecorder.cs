namespace HotChocolate.Fusion.Authorization.InMemory;

/// <summary>
/// Records every entry the in-memory policies were asked about, in evaluation order.
/// </summary>
public sealed class InMemoryPolicyRecorder
{
    private readonly object _sync = new();
    private readonly List<InMemoryPolicyRecord> _records = [];

    /// <summary>
    /// Gets a snapshot of the recorded entries in evaluation order.
    /// </summary>
    public IReadOnlyList<InMemoryPolicyRecord> Records
    {
        get
        {
            lock (_sync)
            {
                return [.. _records];
            }
        }
    }

    /// <summary>
    /// Removes all recorded entries.
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            _records.Clear();
        }
    }

    internal void Record(string policyName, in PolicyEvaluationEntry entry)
    {
        lock (_sync)
        {
            _records.Add(new InMemoryPolicyRecord(policyName, entry));
        }
    }
}
