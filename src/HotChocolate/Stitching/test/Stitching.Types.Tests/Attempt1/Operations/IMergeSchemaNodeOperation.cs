namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public interface IMergeSchemaNodeOperation
{
    void Apply(ISchemaNode source, ISchemaNode target, MergeOperationContext operationContext);
}

internal interface ISchemaNodeOperation<in TDefinition> : IMergeSchemaNodeOperation
{
    void IMergeSchemaNodeOperation.Apply(ISchemaNode source, ISchemaNode target, MergeOperationContext context)
        => Apply((TDefinition)source, (TDefinition)target, context);

    void Apply(TDefinition source, TDefinition target, MergeOperationContext context);
}

internal interface ISchemaNodeRewriteOperation
{
    bool CanHandle(ISchemaNode node, RewriteOperationContext context);

    void Handle(ISchemaNode node, RewriteOperationContext context);
}
