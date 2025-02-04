using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionTypeDirectiveDefinition : DirectiveDefinition
{
    public FusionTypeDirectiveDefinition(EnumTypeDefinition schemaEnumType) : base(FusionType)
    {
        Arguments.Add(new InputFieldDefinition(Schema, new NonNullTypeDefinition(schemaEnumType)));

        IsRepeatable = true;

        Locations =
            DirectiveLocation.Enum
            | DirectiveLocation.InputObject
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar
            | DirectiveLocation.Union;
    }
}
