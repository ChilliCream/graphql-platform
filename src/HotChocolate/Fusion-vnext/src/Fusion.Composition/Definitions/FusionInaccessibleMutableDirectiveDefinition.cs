using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionInaccessibleMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionInaccessibleMutableDirectiveDefinition() : base(FusionInaccessible)
    {
        Locations =
            DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.Enum
            | DirectiveLocation.EnumValue
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.InputObject
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar
            | DirectiveLocation.Union;
    }
}
