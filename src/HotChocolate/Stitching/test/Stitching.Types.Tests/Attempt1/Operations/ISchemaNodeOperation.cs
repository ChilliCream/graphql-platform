namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal interface ISchemaNodeOperation<in TDefinition> : IMergeSchemaNodeOperation
{
    void IMergeSchemaNodeOperation.Apply(ISchemaNode source, ISchemaNode target, MergeOperationContext context)
        => Apply((TDefinition)source, (TDefinition)target, context);

    void Apply(TDefinition source, TDefinition target, MergeOperationContext context);
}