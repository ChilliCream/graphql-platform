using HotChocolate.Language;
using HotChocolate.Stitching.Types.Helpers;

namespace HotChocolate.Stitching.Types;

internal class MergeFieldDefinitionOperation : ISchemaNodeOperation<FieldDefinitionNode>
{
    public FieldDefinitionNode Apply(FieldDefinitionNode source, FieldDefinitionNode target, OperationContext context)
    {
        target = this.MergeDirectives(source, target, target.WithDirectives);
        return target;
    }
}
