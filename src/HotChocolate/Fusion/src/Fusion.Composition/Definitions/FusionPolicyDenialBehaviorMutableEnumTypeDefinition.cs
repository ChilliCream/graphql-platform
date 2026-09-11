using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionPolicyDenialBehaviorMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public FusionPolicyDenialBehaviorMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.FusionPolicyDenialBehavior)
    {
        Values.Add(new MutableEnumValue("NULL"));
        Values.Add(new MutableEnumValue("ERROR"));
        Values.Add(new MutableEnumValue("ABORT"));
    }
}
