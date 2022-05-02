using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeObjectTypeDefinitionOperation : ISchemaNodeOperation<ObjectTypeDefinition>
{
    public void Apply(ObjectTypeDefinition source, ObjectTypeDefinition target, MergeOperationContext context)
    {
        source.MergeInterfacesInto(target);
        source.MergeDirectivesInto(target);
    }
}
