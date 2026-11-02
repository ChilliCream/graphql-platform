using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__inaccessible</c> directive is used to prevent specific type system members from
/// being accessible through the client-facing composite schema, even if they are accessible in the
/// underlying source schemas.
/// </summary>
internal sealed class FusionInaccessibleMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionInaccessibleMutableDirectiveDefinition() : base(FusionInaccessible)
    {
        Description = FusionInaccessibleMutableDirectiveDefinition_Description;

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
