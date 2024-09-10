#if NET6_0_OR_GREATER
using System.Collections.Immutable;
using HotChocolate.Types;

namespace HotChocolate.Execution.Projections;

internal sealed class FieldRequirementsMetadata
{
    private readonly Dictionary<SchemaCoordinate,  ImmutableArray<PropertyNode>> _allRequirements = new();
    private bool _sealed;

    public ImmutableArray<PropertyNode>? GetRequirements(IObjectField field)
        => _allRequirements.TryGetValue(field.Coordinate, out var requirements) ? requirements : null;

    public void TryAddRequirements(SchemaCoordinate fieldCoordinate, ImmutableArray<PropertyNode> requirements)
    {
        if(_sealed)
        {
            throw new InvalidOperationException("The requirements are sealed.");
        }

        _allRequirements.TryAdd(fieldCoordinate, requirements);
    }

    public void Seal()
        => _sealed = true;
}
#endif
