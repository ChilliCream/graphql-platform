using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class SchemaOperations
{
    private readonly List<ISchemaNodeRewriteOperation> _operations;
    private readonly ISchemaDatabase _schemaDatabase;

    public SchemaOperations(List<ISchemaNodeRewriteOperation> operations, ISchemaDatabase schemaDatabase)
    {
        _operations = operations;
        _schemaDatabase = schemaDatabase;
    }

    public void Apply(DocumentDefinition documentDefinition)
    {
        foreach (ISchemaNodeRewriteOperation operation in _operations)
        {
            IEnumerable<ISchemaNode> nodes = documentDefinition
                .DescendentNodes(_schemaDatabase);

            foreach (ISchemaNode node in nodes)
            {
                if (!operation.CanHandle(node))
                {
                    continue;
                }

                operation.Handle(node);
            }
        }
    }
}
