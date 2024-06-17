using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Types;

public sealed class RequireDirectiveDefinition : DirectiveDefinition
{
    internal RequireDirectiveDefinition(FieldSelectionMapType fieldSelectionMapType)
        : base(CompositeSchemaSpec.Require.Name)
    {
        Locations = DirectiveLocation.ArgumentDefinition;
        Arguments.Add(new InputFieldDefinition(CompositeSchemaSpec.Require.Field, fieldSelectionMapType));
    }

    public InputFieldDefinition Field => Arguments[CompositeSchemaSpec.Require.Field];
}
