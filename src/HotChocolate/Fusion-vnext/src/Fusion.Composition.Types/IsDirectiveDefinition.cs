using HotChocolate.Skimmed;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Composition.Types;

public sealed class IsDirectiveDefinition : DirectiveDefinition
{
    internal IsDirectiveDefinition(FieldSelectionMapType fieldSelectionMapType)
        : base(CompositeSchemaSpec.Is.Name)
    {
        Locations = DirectiveLocation.ArgumentDefinition;
        Arguments.Add(new InputFieldDefinition(CompositeSchemaSpec.Is.Field, fieldSelectionMapType));
    }

    public InputFieldDefinition Field => Arguments[CompositeSchemaSpec.Is.Field];
}
