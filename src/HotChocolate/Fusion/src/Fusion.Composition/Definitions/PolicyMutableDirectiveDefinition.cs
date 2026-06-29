using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class PolicyMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public PolicyMutableDirectiveDefinition(
        MutableScalarTypeDefinition stringType,
        MutableEnumTypeDefinition policyDenialBehaviorType)
        : base(WellKnownDirectiveNames.Policy)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Name,
                new NonNullType(stringType)));
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.OnDenied,
                new NonNullType(policyDenialBehaviorType))
            {
                DefaultValue = new EnumValueNode("NULL")
            });
        IsRepeatable = true;
        Locations =
            DirectiveLocation.Object
            | DirectiveLocation.Interface
            | DirectiveLocation.FieldDefinition;
    }

    public static PolicyMutableDirectiveDefinition Create(ISchemaDefinition schema)
    {
        if (!schema.Types.TryGetType<MutableScalarTypeDefinition>(
                SpecScalarNames.String.Name,
                out var stringType))
        {
            stringType = BuiltIns.String.Create();
        }

        if (!schema.Types.TryGetType<MutableEnumTypeDefinition>(
            WellKnownTypeNames.PolicyDenialBehavior,
            out var policyDenialBehaviorType))
        {
            policyDenialBehaviorType = PolicyDenialBehaviorMutableEnumTypeDefinition.Create();
        }

        return new PolicyMutableDirectiveDefinition(stringType, policyDenialBehaviorType);
    }
}
