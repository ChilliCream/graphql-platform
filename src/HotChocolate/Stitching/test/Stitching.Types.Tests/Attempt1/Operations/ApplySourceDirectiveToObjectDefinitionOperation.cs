using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveToObjectDefinitionOperation : ApplySourceDirectiveOperationBase, ISchemaNodeOperation<ObjectTypeDefinition>
{
    public void Apply(ObjectTypeDefinition source, ObjectTypeDefinition target, MergeOperationContext context)
    {
        ApplySourceDirective<ObjectTypeDefinition, ObjectTypeDefinitionNode>(source, target, context);
    }
}
