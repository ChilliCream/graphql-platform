using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@authenticated</c> directive marks a type system member as requiring an authenticated
/// client.
/// </summary>
internal sealed class AuthenticatedMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public AuthenticatedMutableDirectiveDefinition() : base(WellKnownDirectiveNames.Authenticated)
    {
        Description = AuthenticatedMutableDirectiveDefinition_Description;

        Locations =
            DirectiveLocation.Enum
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar;
    }
}
