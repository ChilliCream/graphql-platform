using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@policy</c> directive requires the client to satisfy all policies of at least one of
/// the given policy groups.
/// </summary>
internal sealed class PolicyMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public PolicyMutableDirectiveDefinition(MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.Policy)
    {
        Description = PolicyMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Policies,
                new NonNullType(new ListType(new NonNullType(new ListType(new NonNullType(stringType))))))
            {
                Description = PolicyMutableDirectiveDefinition_Argument_Policies_Description
            });

        Locations =
            DirectiveLocation.Enum
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar;
    }
}
