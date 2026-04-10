using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution;

public readonly record struct VariableValues(CompactPath Path, JsonSegment Values)
{
    public bool IsEmpty => Values.IsEmpty;

    /// <summary>
    /// Gets the additional paths that share the same variable values as the primary <see cref="Path"/>.
    /// </summary>
    public CompactPathSegment AdditionalPaths { get; init; }

    public static VariableValues Empty => default;
}
