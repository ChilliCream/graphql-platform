using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@fusion__eventStream</c> directive specifies broker metadata for a composed
/// subscription field.
/// </summary>
internal sealed class FusionEventStreamMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public FusionEventStreamMutableDirectiveDefinition(
        MutableEnumTypeDefinition schemaMutableEnumType,
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition stringType)
        : base(FusionEventStream)
    {
        Arguments.Add(new MutableInputFieldDefinition(Schema, new NonNullType(schemaMutableEnumType)));
        Arguments.Add(new MutableInputFieldDefinition(Topics, new ListType(new NonNullType(stringType))));
        Arguments.Add(new MutableInputFieldDefinition(Broker, stringType));
        Arguments.Add(new MutableInputFieldDefinition(Message, new NonNullType(fieldSelectionSetType)));
        Arguments.Add(new MutableInputFieldDefinition(CursorField, stringType));
        Arguments.Add(new MutableInputFieldDefinition(CursorArgument, stringType));

        Locations = DirectiveLocation.FieldDefinition;
    }
}
