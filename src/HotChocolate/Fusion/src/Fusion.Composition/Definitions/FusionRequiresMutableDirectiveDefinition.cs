using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__requires</c> directive specifies if a field has requirements on a source schema.
/// </summary>
internal sealed class FusionRequiresMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionRequiresMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition fieldDefinitionType,
        MutableScalarTypeDefinition fieldSelectionMapType)
        : base(FusionRequires)
    {
        Description = FusionRequiresMutableDirectiveDefinition_Description;

        Arguments.Add(
            new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType))
            {
                Description = FusionRequiresMutableDirectiveDefinition_Schema_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(Requirements, new NonNullType(fieldSelectionSetType))
            {
                Description = FusionRequiresMutableDirectiveDefinition_Requirements_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(Field, new NonNullType(fieldDefinitionType))
            {
                Description = FusionRequiresMutableDirectiveDefinition_Field_Description
            });

        Arguments.Add(
            new MutableInputFieldDefinition(
                Map,
                new NonNullType(new ListType(fieldSelectionMapType)))
            {
                Description = FusionRequiresMutableDirectiveDefinition_Map_Description
            });

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
