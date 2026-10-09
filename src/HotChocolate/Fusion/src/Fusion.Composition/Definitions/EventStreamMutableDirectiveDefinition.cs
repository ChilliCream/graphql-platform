using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@eventStream</c> directive declares event-stream metadata for a subscription field.
/// </summary>
internal sealed class EventStreamMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public EventStreamMutableDirectiveDefinition(
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.EventStream)
    {
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Topics,
                new ListType(new NonNullType(stringType))));
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Broker,
                stringType));
        Arguments.Add(
            new MutableInputFieldDefinition(
                WellKnownArgumentNames.Message,
                new NonNullType(fieldSelectionSetType)));

        Locations = DirectiveLocation.FieldDefinition;
    }
}
