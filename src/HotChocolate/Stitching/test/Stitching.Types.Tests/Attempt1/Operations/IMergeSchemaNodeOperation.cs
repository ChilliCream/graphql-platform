namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public interface IMergeSchemaNodeOperation
{
    void Apply(ISchemaNode source, ISchemaNode target, MergeOperationContext operationContext);
}
