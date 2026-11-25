using System.Collections.Immutable;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Execution;

public readonly record struct RepresentationValue(
    JsonSegment Value,
    ImmutableArray<EntityResultPath> ResultPaths)
{
    public bool IsEmpty => Value.IsEmpty;

    public static RepresentationValue Empty => default;

    /// <summary>
    /// Creates one variable value set per distinct entity so that errors can be
    /// attributed to every result path this representation covers. Each set
    /// shares the combined representations payload.
    /// </summary>
    public ImmutableArray<VariableValues> ToVariableValues()
    {
        var variables = new VariableValues[ResultPaths.Length];

        for (var i = 0; i < ResultPaths.Length; i++)
        {
            (var path, var additionalPaths) = ResultPaths[i];

            variables[i] = new VariableValues(path, Value)
            {
                AdditionalPaths = additionalPaths
            };
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(variables);
    }
}

public readonly record struct EntityResultPath(
    CompactPath Path,
    CompactPathSegment AdditionalPaths)
{
    private readonly CompactPath _path = Path;

    /// <summary>
    /// Gets the result path of the entity.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The result document of the entity is no longer in use.
    /// </exception>
    public CompactPath Path
    {
        get => PathSource is null ? _path : PathSource.CreatePath(PathCursor);
        init
        {
            _path = value;
            PathSource = null;
            PathCursor = 0;
        }
    }

    /// <summary>
    /// Gets the source that creates <see cref="Path"/> from <see cref="PathCursor"/> when it is
    /// read, or <c>null</c> if the path already exists.
    /// </summary>
    internal TargetCursorPathSource? PathSource { get; init; }

    /// <summary>
    /// Gets the cursor of the entity in the result document of <see cref="PathSource"/>.
    /// </summary>
    internal int PathCursor { get; init; }

    /// <summary>
    /// Returns a string with the path of the entity if it exists, or with its cursor otherwise.
    /// </summary>
    public override string ToString()
        => PathSource is null
            ? $"EntityResultPath {{ Path = {_path}, AdditionalPaths = {AdditionalPaths} }}"
            : $"EntityResultPath {{ PathCursor = {PathCursor}, AdditionalPaths = {AdditionalPaths} }}";
}
