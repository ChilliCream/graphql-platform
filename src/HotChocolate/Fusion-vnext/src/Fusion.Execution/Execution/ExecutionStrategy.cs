namespace HotChocolate.Fusion.Execution;

internal enum ExecutionStrategy
{
    /// <summary>
    /// Everything that can be executed in parallel will be executed in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// Root nodes will be executed in sequence, everything else can be parallelized.
    /// </summary>
    SequentialRoots
}
