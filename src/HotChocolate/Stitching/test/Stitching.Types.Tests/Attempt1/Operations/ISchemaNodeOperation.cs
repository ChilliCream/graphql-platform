using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaNodeOperation
{
    ISyntaxNode Apply(ISyntaxNode source, ISyntaxNode target, OperationContext operationContext);
}

internal interface ISchemaNodeOperation<TDefinition> : ISchemaNodeOperation
    where TDefinition : ISyntaxNode
{
    ISyntaxNode ISchemaNodeOperation.Apply(ISyntaxNode source, ISyntaxNode target, OperationContext context)
        => Apply((TDefinition)source, (TDefinition)target, context);

    TDefinition Apply(TDefinition source, TDefinition target, OperationContext context);
}

internal interface ISchemaNodeOperation<in TSourceDefinition, TTargetDefinition> : ISchemaNodeOperation
    where TSourceDefinition : ISyntaxNode
    where TTargetDefinition : ISyntaxNode
{
    ISyntaxNode ISchemaNodeOperation.Apply(ISyntaxNode source, ISyntaxNode target, OperationContext context)
    {
        return Apply((TSourceDefinition)source, (TTargetDefinition)target, context);
    }

    TTargetDefinition Apply(TSourceDefinition source, TTargetDefinition target, OperationContext context);
}
