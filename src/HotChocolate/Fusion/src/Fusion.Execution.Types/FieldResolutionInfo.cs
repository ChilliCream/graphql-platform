using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types;

internal readonly record struct FieldResolutionInfo(
    ImmutableArray<string> Schemas,
    ImmutableArray<string> SchemasWithRequirements)
{
    public bool ContainsSchema(string schemaName)
    {
        foreach (var candidate in Schemas)
        {
            if (candidate.Equals(schemaName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public bool HasRequirements(string schemaName)
    {
        foreach (var candidate in SchemasWithRequirements)
        {
            if (candidate.Equals(schemaName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
