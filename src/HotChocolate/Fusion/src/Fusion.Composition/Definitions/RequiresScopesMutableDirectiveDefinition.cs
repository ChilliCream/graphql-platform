using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@requiresScopes</c> directive requires the client to be granted all scopes of at least
/// one of the given scope groups.
/// </summary>
internal sealed class RequiresScopesMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public RequiresScopesMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.RequiresScopes)
    {
        Description = RequiresScopesMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Scopes,
                new NonNullType(new ListType(new NonNullType(new ListType(new NonNullType(stringType))))))
            {
                Description = RequiresScopesMutableDirectiveDefinition_Argument_Scopes_Description
            });

        Locations =
            DirectiveLocation.Enum
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar;
    }
}
