using System.Collections.Immutable;
using System.Runtime.InteropServices;
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
    CompactPathSegment AdditionalPaths);
