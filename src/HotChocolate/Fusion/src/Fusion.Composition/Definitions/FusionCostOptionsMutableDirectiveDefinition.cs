using HotChocolate.Types.Mutable;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionCostOptionsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionCostOptionsMutableDirectiveDefinition(
        MutableScalarTypeDefinition intType,
        MutableScalarTypeDefinition floatType,
        MutableScalarTypeDefinition booleanType,
        MutableEnumTypeDefinition caseBudgetExceededBehaviorType)
        : base(WellKnownDirectiveNames.FusionCostOptions)
    {
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.DefaultListSize, intType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.MaxFieldCost, floatType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.MaxTypeCost, floatType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.EnforceCostLimits, booleanType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.SkipAnalyzer, booleanType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.MaxResponseSize, floatType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.CostPlanCacheSize, intType));
        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.CaseBudget, intType));
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.CaseBudgetExceededBehavior,
                caseBudgetExceededBehaviorType));

        Locations = DirectiveLocation.Schema;
    }
}
