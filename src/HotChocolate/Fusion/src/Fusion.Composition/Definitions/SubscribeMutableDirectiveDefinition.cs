using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@subscribe</c> directive declares event-stream metadata for a subscription field.
/// </summary>
internal sealed class SubscribeMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public SubscribeMutableDirectiveDefinition(
        MutableScalarTypeDefinition fieldSelectionSetType,
        MutableScalarTypeDefinition stringType)
        : base(WellKnownDirectiveNames.Subscribe)
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
