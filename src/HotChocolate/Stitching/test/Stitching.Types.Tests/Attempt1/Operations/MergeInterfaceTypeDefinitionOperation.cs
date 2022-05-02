using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeInterfaceTypeDefinitionOperation : ISchemaNodeOperation<InterfaceTypeDefinition>
{
    public void Apply(InterfaceTypeDefinition source, InterfaceTypeDefinition target, MergeOperationContext context)
    {
        source.MergeInterfacesInto(target);
        source.MergeDirectivesInto(target);
    }
}
