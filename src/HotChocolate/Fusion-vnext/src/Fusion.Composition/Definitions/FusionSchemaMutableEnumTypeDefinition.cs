using System.Collections.Immutable;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.StringUtilities;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionSchemaMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public FusionSchemaMutableEnumTypeDefinition(ImmutableArray<string> schemaNames) : base(FusionSchema)
    {
        foreach (var schemaName in schemaNames)
        {
            Values.Add(new MutableEnumValue(ToConstantCase(schemaName)));
        }
    }
}
