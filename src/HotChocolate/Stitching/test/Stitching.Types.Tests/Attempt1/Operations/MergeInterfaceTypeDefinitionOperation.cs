using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeInterfaceTypeDefinitionOperation : IMergeSchemaNodeOperation<InterfaceTypeDefinitionNode, InterfaceTypeDefinition>
{
    public void Apply(InterfaceTypeDefinitionNode source, InterfaceTypeDefinition target, MergeOperationContext context)
    {
        source.MergeInterfacesInto(target, context);
        source.MergeDirectivesInto(target, context);
    }
}
