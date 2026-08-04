using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>fusion__FieldSelectionSet</c> scalar is used to represent a GraphQL selection set. To
/// simplify the syntax, the outermost selection set is not wrapped in curly braces.
/// </summary>
internal sealed class FusionFieldSelectionSetMutableScalarTypeDefinition
    : MutableScalarTypeDefinition
{
    public FusionFieldSelectionSetMutableScalarTypeDefinition() : base(FusionFieldSelectionSet)
    {
        Description = FusionFieldSelectionSetMutableScalarTypeDefinition_Description;
    }
}
