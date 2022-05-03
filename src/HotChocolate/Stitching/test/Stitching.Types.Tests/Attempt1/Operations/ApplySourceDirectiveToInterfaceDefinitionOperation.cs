using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveToInterfaceDefinitionOperation : ApplySourceDirectiveOperationBase, IMergeSchemaNodeOperation<InterfaceTypeDefinitionNode, InterfaceTypeDefinition>
{
    public void Apply(InterfaceTypeDefinitionNode source, InterfaceTypeDefinition target, MergeOperationContext context)
    {
        ApplySourceDirective<InterfaceTypeDefinitionNode, InterfaceTypeDefinition>(target, context);
    }
}
