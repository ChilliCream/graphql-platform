using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeComplexTypeDefinitionNodeBaseDefinitionOperation : IMergeSchemaNodeOperation<ComplexTypeDefinitionNodeBase, ObjectTypeDefinition>
{
    public void Apply(ComplexTypeDefinitionNodeBase source, ObjectTypeDefinition target, MergeOperationContext context)
    {
        source.MergeInterfacesInto(target, context);
        source.MergeDirectivesInto(target, context);
    }
}
