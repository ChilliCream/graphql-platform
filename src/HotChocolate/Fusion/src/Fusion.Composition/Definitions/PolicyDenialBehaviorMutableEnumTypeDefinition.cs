using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class PolicyDenialBehaviorMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public PolicyDenialBehaviorMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.PolicyDenialBehavior)
    {
        Values.Add(new MutableEnumValue("NULL"));
        Values.Add(new MutableEnumValue("ERROR"));
        Values.Add(new MutableEnumValue("ABORT"));
    }

    public static PolicyDenialBehaviorMutableEnumTypeDefinition Create()
    {
        return new PolicyDenialBehaviorMutableEnumTypeDefinition();
    }
}
