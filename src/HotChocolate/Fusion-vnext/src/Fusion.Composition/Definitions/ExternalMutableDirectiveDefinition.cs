using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@external</c> directive indicates that a field is recognized by the current source schema
/// but is not directly contributed (resolved) by it. Instead, this schema references the field for
/// specific composition purposes.
/// </summary>
internal sealed class ExternalMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ExternalMutableDirectiveDefinition() : base(WellKnownDirectiveNames.External)
    {
        Description = ExternalMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.FieldDefinition;
    }
}
