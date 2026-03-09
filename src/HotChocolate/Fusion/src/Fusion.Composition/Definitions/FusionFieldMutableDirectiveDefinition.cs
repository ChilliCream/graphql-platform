using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__field</c> directive specifies which source schema provides a field in a
/// composite type and what execution behavior it has.
/// </summary>
internal sealed class FusionFieldMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionFieldMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType,
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition booleanType) : base(FusionField)
    {
        Description = FusionFieldMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType))
            {
                Description = FusionFieldMutableDirectiveDefinition_Argument_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.SourceType, stringType)
            {
                Description = FusionFieldMutableDirectiveDefinition_Argument_SourceType_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.Provides, fieldSelectionSetType)
            {
                Description = FusionFieldMutableDirectiveDefinition_Argument_Provides_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Partial,
                new NonNullType(booleanType))
            {
                Description = FusionFieldMutableDirectiveDefinition_Argument_Partial_Description,
                DefaultValue = new BooleanValueNode(false)
            });

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
