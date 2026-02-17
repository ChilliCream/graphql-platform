using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed record VariableValues(Path Path, ObjectValueNode Values)
{
    /// <summary>
    /// Gets the additional paths that share the same variable values as the primary <see cref="Path"/>.
    /// </summary>
    public ImmutableArray<Path> AdditionalPaths { get; init; } = [];
}
