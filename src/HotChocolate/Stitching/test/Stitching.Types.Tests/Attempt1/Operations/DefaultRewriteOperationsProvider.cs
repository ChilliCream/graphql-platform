using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class DefaultRewriteOperationsProvider
{
    private readonly List<ISchemaNodeRewriteOperation> _operations =
        new() { new RenameTypeOperation(), new RenameFieldOperation() };

    private readonly ISchemaDatabase _schemaDatabase;

    public DefaultRewriteOperationsProvider(ISchemaDatabase schemaDatabase)
    {
        _schemaDatabase = schemaDatabase;
    }

    public void Apply(ISchemaNode schemaNode)
    {
        var operationContext = new RewriteOperationContext(_schemaDatabase);
        foreach (ISchemaNodeRewriteOperation operation in _operations)
        {
            IEnumerable<ISchemaNode> nodes = schemaNode
                .DescendentNodes(_schemaDatabase);

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
