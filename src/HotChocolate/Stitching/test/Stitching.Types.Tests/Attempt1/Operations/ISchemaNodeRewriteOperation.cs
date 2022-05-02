namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal interface ISchemaNodeRewriteOperation
{
    bool CanHandle(ISchemaNode node, RewriteOperationContext context);

    void Handle(ISchemaNode node, RewriteOperationContext context);
}