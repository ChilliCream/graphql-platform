using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

internal sealed class FusionImplementsMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionImplementsMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition stringType)
        : base(FusionImplements)
    {
        Arguments.Add(new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType)));
        Arguments.Add(new MutableInputFieldDefinition(Interface, new NonNullType(stringType)));

        IsRepeatable = true;
        Locations = DirectiveLocation.Object | DirectiveLocation.Interface;
    }
}
