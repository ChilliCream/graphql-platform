using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>fusion__FieldSelectionMap</c> scalar is used to represent the <c>FieldSelectionMap</c>
/// type specified in the GraphQL Composite Schemas Spec.
/// </summary>
internal sealed class FusionFieldSelectionMapMutableScalarTypeDefinition
    : MutableScalarTypeDefinition
{
    public FusionFieldSelectionMapMutableScalarTypeDefinition() : base(FusionFieldSelectionMap)
    {
        Description = FusionFieldSelectionMapMutableScalarTypeDefinition_Description;
    }
}
