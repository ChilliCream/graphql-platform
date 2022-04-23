using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public interface IMergeSchemaNodeOperation
{
    ISyntaxNode Apply(ISyntaxNode source, ISyntaxNode target, MergeOperationContext operationContext);
}

internal interface ISchemaNodeOperation<TDefinition> : IMergeSchemaNodeOperation
    where TDefinition : ISyntaxNode
{
    ISyntaxNode IMergeSchemaNodeOperation.Apply(ISyntaxNode source, ISyntaxNode target, MergeOperationContext context)
        => Apply((TDefinition)source, (TDefinition)target, context);

    TDefinition Apply(TDefinition source, TDefinition target, MergeOperationContext context);
}

internal interface ISchemaNodeOperation<in TSourceDefinition, TTargetDefinition> : IMergeSchemaNodeOperation
    where TSourceDefinition : ISyntaxNode
    where TTargetDefinition : ISyntaxNode
{
    ISyntaxNode IMergeSchemaNodeOperation.Apply(ISyntaxNode source, ISyntaxNode target, MergeOperationContext context)
    {
        return Apply((TSourceDefinition)source, (TTargetDefinition)target, context);
    }

    TTargetDefinition Apply(TSourceDefinition source, TTargetDefinition target, MergeOperationContext context);
}

internal interface ISchemaNodeRewriteOperation
{
    bool CanHandle(ISchemaNode node, RewriteOperationContext context);

    void Handle(ISchemaNode node, RewriteOperationContext context);
}
