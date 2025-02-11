using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionInputFieldMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionInputFieldMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        ScalarTypeDefinition stringType)
        : base(FusionInputField)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullTypeDefinition(schemaMutableEnumType)));

        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.SourceType, stringType));

        IsRepeatable = true;
        Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition;
    }
}
