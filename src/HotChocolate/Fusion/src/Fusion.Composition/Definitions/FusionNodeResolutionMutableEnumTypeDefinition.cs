using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionNodeResolutionMutableEnumTypeDefinition : MutableEnumTypeDefinition
{
    public FusionNodeResolutionMutableEnumTypeDefinition()
        : base(WellKnownTypeNames.FusionNodeResolution)
    {
        Values.Add(new MutableEnumValue("GATEWAY"));
        Values.Add(new MutableEnumValue("SOURCE_SCHEMA"));
    }
}
