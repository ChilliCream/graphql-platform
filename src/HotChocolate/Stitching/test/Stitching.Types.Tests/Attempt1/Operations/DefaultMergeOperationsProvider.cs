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
            typeof(ObjectTypeDefinitionNode),
            new List<IMergeSchemaNodeOperation>
            {
                new MergeComplexTypeDefinitionNodeBaseDefinitionOperation(),
                new ApplySourceDirectiveToObjectDefinitionOperation<ObjectTypeDefinitionNode, ObjectTypeDefinition>()
            }
        },
        {
            typeof(ObjectTypeExtensionNode),
            new List<IMergeSchemaNodeOperation> { new MergeComplexTypeDefinitionNodeBaseDefinitionOperation() }
        },
        {
            typeof(InterfaceTypeDefinitionNode),
            new List<IMergeSchemaNodeOperation>
            {
                new MergeInterfaceTypeDefinitionOperation(),
                new ApplySourceDirectiveToInterfaceDefinitionOperation()
            }
        },
        {
            typeof(FieldDefinitionNode),
            new List<IMergeSchemaNodeOperation>
            {
                new MergeFieldDefinitionOperation(), new ApplySourceDirectiveToFieldDefinitionOperation()
            }
        }
    };

    public void Apply(SubgraphDocument sourceDefinition, ISchemaNode destinationDefinition)
    {
        var context = new MergeOperationContext(sourceDefinition.Name,
            destinationDefinition.Database.Name);

        Apply(sourceDefinition.Definition,
            destinationDefinition,
            context);
    }

    public void Apply(ISyntaxNode source, ISchemaNode destination)
    {
        Apply(source, destination, new MergeOperationContext());
    }

    private static void Apply(
        ISyntaxNode source,
        ISchemaNode destination,
        MergeOperationContext context)
    {
        IEnumerable<SyntaxNodeReference> childSyntaxNodes = source.DescendentSyntaxNodes();
        foreach (SyntaxNodeReference nodeReference in childSyntaxNodes)
        {
            ISchemaNode targetDefinition = destination.Database.GetOrAdd(nodeReference);

            ICollection<IMergeSchemaNodeOperation> operations = GetOperations(nodeReference);
            foreach (IMergeSchemaNodeOperation operation in operations)
                operation.Apply(nodeReference.Node, targetDefinition, context);
        }
    }

    private static ICollection<IMergeSchemaNodeOperation> GetOperations(SyntaxNodeReference sourceReference)
    {
        ISyntaxNode source = sourceReference.Node;

        ICollection<IMergeSchemaNodeOperation> syntaxNodeOperations =
            _operations.TryGetValue(source.GetType(),
                out ICollection<IMergeSchemaNodeOperation>? nodeOperations)
                ? nodeOperations
                : new List<IMergeSchemaNodeOperation>(0);

        return syntaxNodeOperations;
    }
}
