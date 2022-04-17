using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeObjectTypeExtensionsDefinitionOperation : ISchemaNodeOperation<ObjectTypeExtensionNode, ObjectTypeDefinitionNode>
{
    public ObjectTypeDefinitionNode Apply(ObjectTypeExtensionNode source, ObjectTypeDefinitionNode target, OperationContext context)
    {
        target = this.MergeInterfaces(source, target);
        target = this.MergeDirectives(source, target);
        target = this.MergeFields(source, target, context);
        return target;
    }
}
