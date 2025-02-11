using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionFieldMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionFieldMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        ScalarTypeDefinition stringType,
        ScalarTypeDefinition fieldSelectionSetType,
        ScalarTypeDefinition booleanType) : base(FusionField)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaMutableEnumType)));

        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.SourceType, stringType));

        Arguments.Add(
            new MutableInputFieldDefinition(WellKnownArgumentNames.Provides, fieldSelectionSetType));

        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.External,
                new NonNullTypeDefinition(booleanType))
                    { DefaultValue = new BooleanValueNode(false) });

        IsRepeatable = true;
        Locations = DirectiveLocation.FieldDefinition;
    }
}
