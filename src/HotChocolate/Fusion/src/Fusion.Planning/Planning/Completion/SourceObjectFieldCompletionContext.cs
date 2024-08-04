using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Directives;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct SourceObjectFieldCompletionContext(
    ImmutableArray<Requirement> directives,
    ICompositeType type)
{
    public ImmutableArray<Requirement> Requirements { get; } = directives;

    public ICompositeType Type { get; } = type;
}
