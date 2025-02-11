using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionRequiresMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionRequiresMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition fieldDefinitionType,
        MutableScalarTypeDefinition fieldSelectionMapType)
        : base(FusionRequires)
    {
        Arguments.Add(new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType)));

        Arguments.Add(
            new MutableInputFieldDefinition(Field, new NonNullType(fieldDefinitionType)));

        Arguments.Add(
            new MutableInputFieldDefinition(
                Map,
                new NonNullType(new ListType(fieldSelectionMapType))));

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
