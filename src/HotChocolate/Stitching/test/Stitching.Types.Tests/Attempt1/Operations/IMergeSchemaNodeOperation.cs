using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

public interface IMergeSchemaNodeOperation
{
    void Apply(ISyntaxNode source, ISchemaNode target, MergeOperationContext operationContext);
}

internal interface IMergeSchemaNodeOperation<in TSource, in TTarget> : IMergeSchemaNodeOperation
    where TSource : ISyntaxNode
    where TTarget : ISchemaNode
{
    void IMergeSchemaNodeOperation.Apply(ISyntaxNode source, ISchemaNode target, MergeOperationContext context)
        => Apply((TSource)source, (TTarget)target, context);

    void Apply(TSource source, TTarget target, MergeOperationContext context);
}
