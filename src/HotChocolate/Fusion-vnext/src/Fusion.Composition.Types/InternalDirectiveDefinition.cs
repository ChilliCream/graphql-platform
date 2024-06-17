using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Types;

public sealed class InternalDirectiveDefinition : DirectiveDefinition
{
    internal InternalDirectiveDefinition() : base(CompositeSchemaSpec.Internal.Name)
    {
        Locations = DirectiveLocation.FieldDefinition;
    }
}
