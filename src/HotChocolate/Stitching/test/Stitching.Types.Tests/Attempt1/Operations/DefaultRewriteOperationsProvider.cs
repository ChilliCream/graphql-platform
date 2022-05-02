using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class DefaultRewriteOperationsProvider
{
    private readonly List<ISchemaNodeRewriteOperation> _operations =
        new() { new RenameTypeOperation(), new RenameFieldOperation() };

    public void Apply(ISchemaNode schemaNode)
    {
        var operationContext = new RewriteOperationContext();
        foreach (ISchemaNodeRewriteOperation operation in _operations)
        {
            IEnumerable<ISchemaNode> nodes = schemaNode
                .DescendentNodes();

            foreach (ISchemaNode node in nodes)
            {
                if (!operation.CanHandle(node, operationContext))
                {
                    continue;
                }

                operation.Handle(node, operationContext);
            }
        }
    }
}
