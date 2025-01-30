using System.Collections.Immutable;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.StringUtilities;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionSchemaEnumTypeDefinition : EnumTypeDefinition
{
    public FusionSchemaEnumTypeDefinition(ImmutableArray<string> schemaNames) : base(FusionSchema)
    {
        foreach (var schemaName in schemaNames)
        {
            Values.Add(new EnumValue(ToConstantCase(schemaName)));
        }
    }
}
