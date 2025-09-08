using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@internal</c> directive is used to mark types and fields as internal within a source
/// schema. Internal types and fields do not appear in the final client-facing composite schema and
/// are internal to the source schema they reside in.
/// </summary>
internal sealed class InternalMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public InternalMutableDirectiveDefinition() : base(WellKnownDirectiveNames.Internal)
    {
        Description = InternalMutableDirectiveDefinition_Description;

        Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object;
    }
}
