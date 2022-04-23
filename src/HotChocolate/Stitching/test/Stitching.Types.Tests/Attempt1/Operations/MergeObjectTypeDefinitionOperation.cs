using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeObjectTypeDefinitionOperation : ISchemaNodeOperation<ObjectTypeDefinitionNode>
{
    public ObjectTypeDefinitionNode Apply(ObjectTypeDefinitionNode source, ObjectTypeDefinitionNode target, MergeOperationContext context)
    {
        target = this.MergeInterfaces(source, target);
        target = this.MergeDirectives(source, target);
        target = this.MergeFields(source, target, context);
        return target;
    }
}
