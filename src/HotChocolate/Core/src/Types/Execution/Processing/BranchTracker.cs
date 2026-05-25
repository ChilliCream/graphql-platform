namespace HotChocolate.Execution.Processing;

internal sealed class BranchTracker
{
    private int _nextId;

    public const int SystemBranchId = -1;

    public int CreateNewBranchId()
        => Interlocked.Increment(ref _nextId);

    public void Reset() => _nextId = 0;
}
