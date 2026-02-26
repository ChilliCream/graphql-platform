using System.Collections.Immutable;
using HotChocolate.Buffers;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public readonly struct VariableValues
{
    public VariableValues(
        Path path,
        ReadOnlyMemorySegment variables,
        ObjectValueNode? fileMapVariables = null)
    {
        Path = path;
        Variables = variables;
        FileMapVariables = fileMapVariables;
    }

    public Path Path { get; }

    public ReadOnlyMemorySegment Variables { get; }

    internal ObjectValueNode? FileMapVariables { get; }

    /// <summary>
    /// Gets the additional paths that share the same variable values as the primary <see cref="Path"/>.
    /// </summary>
    public ImmutableArray<Path> AdditionalPaths { get; init; } = [];
}
