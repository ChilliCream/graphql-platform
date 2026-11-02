using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>fusion__FieldDefinition</c> scalar is used to represent a GraphQL field definition
/// specified in the GraphQL spec.
/// </summary>
internal sealed class FusionFieldDefinitionMutableScalarTypeDefinition : MutableScalarTypeDefinition
{
    public FusionFieldDefinitionMutableScalarTypeDefinition() : base(FusionFieldDefinition)
    {
        Description = FusionFieldDefinitionMutableScalarTypeDefinition_Description;
    }
}
