using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@propagateNull</c> directive marks a lookup field whose null result invalidates the
/// entity node resolved by that lookup.
/// </summary>
internal sealed class PropagateNullMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public PropagateNullMutableDirectiveDefinition() : base(WellKnownDirectiveNames.PropagateNull)
    {
        Description = PropagateNullMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.FieldDefinition;
    }
}
