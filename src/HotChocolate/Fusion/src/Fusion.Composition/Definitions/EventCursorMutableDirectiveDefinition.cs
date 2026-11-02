using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@eventCursor</c> directive marks cursor fields and resume arguments for event streams.
/// </summary>
internal sealed class EventCursorMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public EventCursorMutableDirectiveDefinition() : base(WellKnownDirectiveNames.EventCursor)
    {
        Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.FieldDefinition;
    }
}
