using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionEnumValueDirectiveDefinition : DirectiveDefinition
{
    public FusionEnumValueDirectiveDefinition(EnumTypeDefinition schemaEnumType)
        : base(FusionEnumValue)
    {
        Arguments.Add(
            new InputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaEnumType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.EnumValue;
    }
}
