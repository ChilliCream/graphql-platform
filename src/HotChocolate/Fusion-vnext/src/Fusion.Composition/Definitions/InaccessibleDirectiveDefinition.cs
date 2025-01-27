using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class InaccessibleDirectiveDefinition : DirectiveDefinition
{
    public InaccessibleDirectiveDefinition() : base(Inaccessible)
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
