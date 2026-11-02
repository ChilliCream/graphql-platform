using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@lookup</c> directive is used within a <i>source schema</i> to specify output fields that
/// can be used by the <i>distributed GraphQL executor</i> to resolve an entity by a stable key.
/// </summary>
internal sealed class LookupMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public LookupMutableDirectiveDefinition() : base(WellKnownDirectiveNames.Lookup)
    {
        Description = LookupMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.FieldDefinition;
    }
}
