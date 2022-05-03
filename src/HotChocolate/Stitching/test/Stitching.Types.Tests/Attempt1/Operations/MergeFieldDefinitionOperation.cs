using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeFieldDefinitionOperation : IMergeSchemaNodeOperation<FieldDefinitionNode, FieldDefinition>
{
    public void Apply(FieldDefinitionNode source, FieldDefinition target, MergeOperationContext context)
    {
        source.MergeDirectivesInto(target, context);
    }
}
