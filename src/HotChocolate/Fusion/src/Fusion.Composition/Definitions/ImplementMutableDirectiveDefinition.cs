using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@implement</c> directive marks a field as an explicit implementation that replaces a
/// default field implementation contributed by an <c>@interfaceObject</c> stand-in.
/// </summary>
internal sealed class ImplementMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ImplementMutableDirectiveDefinition() : base(WellKnownDirectiveNames.Implement)
    {
        Description = ImplementMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.FieldDefinition;
    }
}
