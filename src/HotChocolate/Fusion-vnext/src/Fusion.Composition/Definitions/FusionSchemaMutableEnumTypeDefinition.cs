using System.Collections.Immutable;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.StringUtilities;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>fusion__Schema</c> enum is a generated type used within an execution schema document to
/// refer to a source schema in a type-safe manner.
/// </summary>
internal sealed class FusionSchemaMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public FusionSchemaMutableEnumTypeDefinition(ImmutableArray<string> schemaNames)
        : base(FusionSchema)
    {
        Description = FusionSchemaMutableEnumTypeDefinition_Description;

        foreach (var schemaName in schemaNames)
        {
            Values.Add(new MutableEnumValue(ToConstantCase(schemaName)));
        }
    }
}
