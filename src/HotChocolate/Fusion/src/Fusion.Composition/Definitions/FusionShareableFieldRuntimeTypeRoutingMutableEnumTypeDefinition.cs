using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionShareableFieldRuntimeTypeRoutingMutableEnumTypeDefinition
    : MutableEnumTypeDefinition
{
    public FusionShareableFieldRuntimeTypeRoutingMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.FusionShareableFieldRuntimeTypeRouting)
    {
        Values.Add(new MutableEnumValue("SOURCE_LOCAL"));
        Values.Add(new MutableEnumValue("COMMON_RUNTIME_TYPES"));
    }
}
