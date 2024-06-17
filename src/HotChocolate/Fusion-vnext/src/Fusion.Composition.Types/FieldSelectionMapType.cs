using HotChocolate.Skimmed;

namespace HotChocolate.Fusion.Composition.Types;

public sealed class FieldSelectionMapType : ScalarTypeDefinition
{
    internal FieldSelectionMapType()
        : base(CompositeSchemaSpec.FieldSelectionMap.Name)
    {

    }
}
