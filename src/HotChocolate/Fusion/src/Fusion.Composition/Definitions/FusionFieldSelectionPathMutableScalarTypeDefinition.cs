using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>fusion__FieldSelectionPath</c> scalar is used to represent a path of field names relative
/// to the Query type.
/// </summary>
internal sealed class FusionFieldSelectionPathMutableScalarTypeDefinition
    : MutableScalarTypeDefinition
{
    public FusionFieldSelectionPathMutableScalarTypeDefinition() : base(FusionFieldSelectionPath)
    {
        Description = FusionFieldSelectionPathMutableScalarTypeDefinition_Description;
    }
}
