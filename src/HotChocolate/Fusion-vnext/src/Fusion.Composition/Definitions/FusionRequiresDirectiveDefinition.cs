using HotChocolate.Skimmed;
using HotChocolate.Types;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionRequiresDirectiveDefinition : DirectiveDefinition
{
    public FusionRequiresDirectiveDefinition(
        EnumTypeDefinition schemaEnumType,
        ScalarTypeDefinition fieldDefinitionType,
        ScalarTypeDefinition fieldSelectionMapType)
        : base(FusionRequires)
    {
        Arguments.Add(new InputFieldDefinition(Schema, new NonNullTypeDefinition(schemaEnumType)));

        Arguments.Add(
            new InputFieldDefinition(Field, new NonNullTypeDefinition(fieldDefinitionType)));

        Arguments.Add(
            new InputFieldDefinition(
                Map,
                new NonNullTypeDefinition(new ListTypeDefinition(fieldSelectionMapType))));

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
