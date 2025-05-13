using HotChocolate.Types;

namespace HotChocolate.Execution.Requirements;

internal sealed class FieldRequirementsMetadata
{
    private readonly Dictionary<SchemaCoordinate, TypeNode> _allRequirements = [];
    private bool _sealed;

    public TypeNode? GetRequirements(IOutputFieldDefinition field)
        => _allRequirements.GetValueOrDefault(field.Coordinate);

    public void TryAddRequirements(SchemaCoordinate fieldCoordinate, TypeNode requirements)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("The requirements are sealed.");
        }

        _allRequirements.TryAdd(fieldCoordinate, requirements);
    }

    public void Seal() => _sealed = true;
}
