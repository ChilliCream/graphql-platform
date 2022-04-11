using HotChocolate.Language;
using HotChocolate.Stitching.Types.Helpers;

namespace HotChocolate.Stitching.Types;

internal class MergeObjectTypeExtensionsDefinitionOperation : ISchemaNodeOperation<ObjectTypeExtensionNode, ObjectTypeDefinitionNode>
{
    public ObjectTypeDefinitionNode Apply(ObjectTypeExtensionNode source, ObjectTypeDefinitionNode target, OperationContext context)
    {
        target = this.MergeInterfaces(source, target, target.WithInterfaces);
        target = this.MergeDirectives(source, target, target.WithDirectives);
        target = this.MergeFields(source, target, context, target.WithFields);
        return target;
    }
}
