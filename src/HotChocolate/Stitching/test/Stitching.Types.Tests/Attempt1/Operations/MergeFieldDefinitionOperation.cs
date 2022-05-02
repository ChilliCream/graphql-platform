using HotChocolate.Stitching.Types.Attempt1.Helpers;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class MergeFieldDefinitionOperation : ISchemaNodeOperation<FieldDefinition>
{
    public void Apply(FieldDefinition source, FieldDefinition target, MergeOperationContext context)
    {
        source.MergeDirectivesInto(target);
    }
}
