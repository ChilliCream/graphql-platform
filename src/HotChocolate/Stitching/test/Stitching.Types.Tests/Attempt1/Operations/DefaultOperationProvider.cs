using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal class DefaultOperationProvider : IOperationProvider
{
    private static readonly Dictionary<Type, ICollection<ISchemaNodeOperation>> _operations = new()
    {
        {
            typeof(DocumentNode),
            new List<ISchemaNodeOperation> { new MergeDocumentDefinitionOperation() }
        },
        {
            typeof(ObjectTypeDefinitionNode),
            new List<ISchemaNodeOperation> { new MergeObjectTypeDefinitionOperation() }
        },
        {
            typeof(ObjectTypeExtensionNode),
            new List<ISchemaNodeOperation> { new MergeObjectTypeExtensionsDefinitionOperation() }
        },
        {
            typeof(FieldDefinitionNode),
            new List<ISchemaNodeOperation> { new MergeFieldDefinitionOperation() }
        },
    };

    public ICollection<ISchemaNodeOperation> GetOperations(ISyntaxNode source)
    {
        if (!_operations.TryGetValue(source.GetType(), out ICollection<ISchemaNodeOperation>? operations))
        {
            return new List<ISchemaNodeOperation>(0);
        }

        return operations;
    }
}
