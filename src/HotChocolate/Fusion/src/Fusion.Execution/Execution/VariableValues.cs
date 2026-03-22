using System.Collections.Immutable;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed record VariableValues(CompactPath Path, ObjectValueNode Values)
{
    /// <summary>
    /// Gets the additional paths that share the same variable values as the primary <see cref="Path"/>.
    /// </summary>
    public ImmutableArray<CompactPath> AdditionalPaths { get; init; } = [];
}
