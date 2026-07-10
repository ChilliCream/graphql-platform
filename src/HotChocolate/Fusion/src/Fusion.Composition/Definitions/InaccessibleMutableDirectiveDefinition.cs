using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@inaccessible</c> directive is used to prevent specific type system members from being
/// accessible through the client-facing <i>composite schema</i>, even if they are accessible in the
/// underlying source schemas.
/// </summary>
internal sealed class InaccessibleMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public InaccessibleMutableDirectiveDefinition() : base(WellKnownDirectiveNames.Inaccessible)
    {
        Description = InaccessibleMutableDirectiveDefinition_Description;

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
