using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class ApplyPolicyMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public ApplyPolicyMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.ApplyPolicy)
    {
        Values.Add(new MutableEnumValue("BEFORE_RESOLVER"));
        Values.Add(new MutableEnumValue("AFTER_RESOLVER"));
        Values.Add(new MutableEnumValue("VALIDATION"));
    }

    public static ApplyPolicyMutableEnumTypeDefinition Create()
    {
        return new ApplyPolicyMutableEnumTypeDefinition();
    }
}
