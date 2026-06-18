namespace HotChocolate.Buffers;

// The memory arena and the table pool share a process-wide bucket of segment tables. The tests that
// assert recycling identity rent and return against that shared bucket, so they must not run in
// parallel with one another or a concurrent rent could steal the table under assertion.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MemorySegmentTablePoolCollection
{
    public const string Name = "MemorySegmentTablePool";
}
