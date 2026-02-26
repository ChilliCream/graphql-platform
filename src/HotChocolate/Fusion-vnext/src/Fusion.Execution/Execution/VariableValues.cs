using System;
using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public sealed record VariableValues(Path Path, ObjectValueNode Values)
{
    private static readonly ObjectValueNode s_placeholderValues = new([]);

    /// <summary>
    /// Gets the additional paths that share the same variable values as the primary <see cref="Path"/>.
    /// </summary>
    public ImmutableArray<Path> AdditionalPaths { get; init; } = [];

    /// <summary>
    /// Gets serialized JSON bytes for <see cref="Values"/> when produced by the fast-path mapper.
    /// </summary>
    public ReadOnlyMemory<byte> SerializedValues { get; init; }

    public bool HasSerializedValues => !SerializedValues.IsEmpty;

    internal static VariableValues CreateSerialized(Path path)
        => new(path, s_placeholderValues);
}
