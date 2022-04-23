using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeFieldDefinitionOperation : ISchemaNodeOperation<FieldDefinitionNode>
{
    public FieldDefinitionNode Apply(FieldDefinitionNode source, FieldDefinitionNode target, MergeOperationContext context)
    {
        target = this.MergeDirectives(source, target);
        return target;
    }
}
