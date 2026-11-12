using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionCaseBudgetExceededBehaviorMutableEnumTypeDefinition
    : MutableEnumTypeDefinition
{
    public FusionCaseBudgetExceededBehaviorMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.FusionCaseBudgetExceededBehavior)
    {
        Values.Add(new MutableEnumValue("EVALUATE_PER_REQUEST"));
        Values.Add(new MutableEnumValue("OVERESTIMATE"));
    }
}
