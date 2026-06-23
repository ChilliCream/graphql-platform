using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__subscribe</c> directive specifies broker metadata for a composed
/// subscription field.
/// </summary>
internal sealed class FusionSubscribeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionSubscribeMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition stringType)
        : base(FusionSubscribe)
    {
        Arguments.Add(new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType)));
        Arguments.Add(new MutableInputFieldDefinition(Topics, new ListType(new NonNullType(stringType))));
        Arguments.Add(new MutableInputFieldDefinition(Broker, stringType));
        Arguments.Add(new MutableInputFieldDefinition(Message, new NonNullType(fieldSelectionSetType)));

        Locations = DirectiveLocation.FieldDefinition;
    }
}
