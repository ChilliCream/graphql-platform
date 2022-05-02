using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveToFieldDefinitionOperation : ApplySourceDirectiveOperationBase, ISchemaNodeOperation<FieldDefinition>
{
    public void Apply(FieldDefinition source, FieldDefinition target, MergeOperationContext context)
    {
        ApplySourceDirective<FieldDefinition, FieldDefinitionNode>(source, target, context);
    }
}
