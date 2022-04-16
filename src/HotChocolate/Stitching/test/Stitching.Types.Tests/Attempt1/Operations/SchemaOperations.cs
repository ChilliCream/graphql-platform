using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class SchemaOperations
{
    private readonly List<ISchemaNodeRewriteOperation> _operations;
    private readonly ISchemaDatabase _coordinateProvider;

    public SchemaOperations(List<ISchemaNodeRewriteOperation> operations, ISchemaDatabase coordinateProvider)
    {
        _operations = operations;
        _coordinateProvider = coordinateProvider;
    }

    public void Apply(DocumentDefinition documentDefinition)
    {
        IEnumerable<ISchemaNode> nodes = documentDefinition
            .DescendentNodes(_coordinateProvider);

        foreach (ISchemaNode node in nodes)
        {
            foreach (ISchemaNodeRewriteOperation operation in _operations)
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
