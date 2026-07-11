using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.Definitions;

/// <summary>
/// The <c>@shareable</c> directive allows multiple source schemas to define the same field,
/// ensuring that this decision is both intentional and coordinated by requiring fields to be
/// explicitly marked.
/// </summary>
internal sealed class ShareableMutableDirectiveDefinition : MutableDirectiveDefinition
{
    public ShareableMutableDirectiveDefinition() : base(WellKnownDirectiveNames.Shareable)
    {
        Description = ShareableMutableDirectiveDefinition_Description;

        IsRepeatable = true;

        Locations = DirectiveLocation.FieldDefinition | DirectiveLocation.Object;
    }
}
