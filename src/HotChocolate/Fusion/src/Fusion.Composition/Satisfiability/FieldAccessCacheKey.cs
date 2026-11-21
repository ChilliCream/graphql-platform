using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

internal sealed class FieldAccessCacheKey(
    MutableOutputFieldDefinition field,
    MutableObjectTypeDefinition type,
    string? schemaName,
    string? excludedSchemaName,
    bool isAccessPathItem)
{
    private readonly MutableOutputFieldDefinition _field = field;
    private readonly MutableObjectTypeDefinition _type = type;
    private readonly string? _schemaName = schemaName;
    private readonly string? _excludedSchemaName = excludedSchemaName;
    private readonly bool _isAccessPathItem = isAccessPathItem;
    private readonly int _hashCode = HashCode.Combine(field, type, schemaName, excludedSchemaName, isAccessPathItem);

    public override int GetHashCode() => _hashCode;

    public override bool Equals(object? obj)
    {
        if (obj is not FieldAccessCacheKey other)
        {
            return false;
        }

        return _field == other._field
            && _type == other._type
            && _schemaName == other._schemaName
            && _excludedSchemaName == other._excludedSchemaName
            && _isAccessPathItem == other._isAccessPathItem;
    }
}
