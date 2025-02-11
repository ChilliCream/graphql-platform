using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionInputFieldMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionInputFieldMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionInputField)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Schema,
                new NonNullType(schemaMutableEnumType)));

        Arguments.Add(new MutableInputFieldDefinition(WellKnownArgumentNames.SourceType, stringType));

        IsRepeatable = true;
        Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.InputFieldDefinition;
    }
}
