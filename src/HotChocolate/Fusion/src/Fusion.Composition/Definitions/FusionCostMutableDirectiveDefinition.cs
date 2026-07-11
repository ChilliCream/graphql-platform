using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__cost</c> directive specifies cost metadata for each source schema.
/// </summary>
internal sealed class FusionCostMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionCostMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionCost)
    {
        Description = FusionCostMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionCostMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(Weight, new NonNullType(stringType))
            {
                Description = FusionCostMutableDirectiveDefinition_Argument_Weight_Description
            });

        IsRepeatable = true;

        Locations =
            DirectiveLocation.ArgumentDefinition
            | DirectiveLocation.Enum
            | DirectiveLocation.FieldDefinition
            | DirectiveLocation.InputFieldDefinition
            | DirectiveLocation.Object
            | DirectiveLocation.Scalar;
    }
}
