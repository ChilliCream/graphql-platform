using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__policy</c> directive carries the merged policy requirement of a
/// type system member.
/// </summary>
internal sealed class FusionPolicyMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionPolicyMutableDirectiveDefinition(
        MutableScalarTypeDefinition stringType,
        MutableScalarTypeDefinition booleanType)
        : base(FusionPolicy)
    {
        Description = FusionPolicyMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(ArgumentNames.Authenticated, new NonNullType(booleanType))
            {
                DefaultValue = BooleanValueNode.False,
                Description = FusionPolicyMutableDirectiveDefinition_Argument_Authenticated_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Scopes,
                new ListType(new NonNullType(new ListType(new NonNullType(stringType)))))
            {
                Description = FusionPolicyMutableDirectiveDefinition_Argument_Scopes_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                ArgumentNames.Policies,
                new ListType(new NonNullType(new ListType(new NonNullType(stringType)))))
            {
                Description = FusionPolicyMutableDirectiveDefinition_Argument_Policies_Description
            });

        Locations =
            DirectiveLocation.Enum
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.Interface
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar;
    }
}
