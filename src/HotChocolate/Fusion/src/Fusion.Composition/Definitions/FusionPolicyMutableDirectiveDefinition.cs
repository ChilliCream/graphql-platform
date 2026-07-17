using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionPolicyMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionPolicyMutableDirectiveDefinition(
        MutableScalarTypeDefinition stringType,
        MutableEnumTypeDefinition policyDenialBehaviorType)
        : base(WellKnownDirectiveNames.FusionPolicy)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Names,
                new NonNullType(
                    new ListType(
                        new NonNullType(
                            new ListType(
                                new NonNullType(stringType)))))));
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.OnDenied,
                new NonNullType(policyDenialBehaviorType))
            {
                DefaultValue = new EnumValueNode("NULL")
            });
        IsRepeatable = true;
        Locations = DirectiveLocation.Object | DirectiveLocation.FieldDefinition;
    }
}
