using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionRequiresMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionRequiresMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        ScalarTypeDefinition fieldDefinitionType,
        ScalarTypeDefinition fieldSelectionMapType)
        : base(FusionRequires)
    {
        Arguments.Add(new MutableInputFieldDefinition(Schema, new NonNullTypeDefinition(schemaMutableEnumType)));

        Arguments.Add(
            new MutableInputFieldDefinition(Field, new NonNullTypeDefinition(fieldDefinitionType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                Map,
                new NonNullTypeDefinition(new ListTypeDefinition(fieldSelectionMapType))));

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
