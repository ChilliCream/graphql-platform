using System.Collections.Immutable;

namespace HotChocolate.Execution;

internal sealed class IncrementalDataFeature
{
    public Path? Path { get; set; }

    public ImmutableList<PendingResult>? Pending { get; set; }

    public ImmutableList<IIncrementalResult>? Incremental { get; set; }

    public ImmutableList<CompletedResult>? Completed { get; set; }

    public bool? HasNext { get; set; }
}
