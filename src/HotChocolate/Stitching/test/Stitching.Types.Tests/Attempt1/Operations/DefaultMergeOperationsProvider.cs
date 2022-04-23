using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Attempt1.Wip;

namespace HotChocolate.Stitching.Types.Attempt1.Operations;

internal class DefaultMergeOperationsProvider : IMergeOperationsProvider
{
    private static readonly Dictionary<Type, ICollection<IMergeSchemaNodeOperation>> _operations = new()
    {
        {
            typeof(DocumentNode),
            new List<IMergeSchemaNodeOperation> { new MergeDocumentDefinitionOperation() }
        },
        {
            typeof(ObjectTypeDefinitionNode),
            new List<IMergeSchemaNodeOperation> { new MergeObjectTypeDefinitionOperation() }
        },
        {
            typeof(ObjectTypeExtensionNode),
            new List<IMergeSchemaNodeOperation> { new MergeObjectTypeExtensionsDefinitionOperation() }
        },
        {
            typeof(FieldDefinitionNode),
            new List<IMergeSchemaNodeOperation> { new MergeFieldDefinitionOperation() }
        },
    };

    public ICollection<IMergeSchemaNodeOperation> GetOperations(ISyntaxNode source)
    {
        if (!_operations.TryGetValue(source.GetType(), out ICollection<IMergeSchemaNodeOperation>? operations))
        {
            return new List<IMergeSchemaNodeOperation>(0);
        }

        return operations;
    }
}
