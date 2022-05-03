using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveToFieldDefinitionOperation : ApplySourceDirectiveOperationBase, IMergeSchemaNodeOperation<FieldDefinitionNode, FieldDefinition>
{
    public void Apply(FieldDefinitionNode source, FieldDefinition target, MergeOperationContext context)
    {
        ApplySourceDirective<FieldDefinitionNode, FieldDefinition>(target, context);
    }
}
