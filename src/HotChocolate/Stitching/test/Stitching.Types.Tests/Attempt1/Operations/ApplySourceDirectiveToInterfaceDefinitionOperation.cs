using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class ApplySourceDirectiveToInterfaceDefinitionOperation : ApplySourceDirectiveOperationBase, ISchemaNodeOperation<InterfaceTypeDefinition>
{
    public void Apply(InterfaceTypeDefinition source, InterfaceTypeDefinition target, MergeOperationContext context)
    {
        ApplySourceDirective<InterfaceTypeDefinition, InterfaceTypeDefinitionNode>(source, target);
    }
}
