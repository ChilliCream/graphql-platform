using System;
using System.Collections.Generic;
using System.Linq;
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
            new List<IMergeSchemaNodeOperation> { new ApplySourceDirectiveToObjectDefinitionOperation() }
        },
        {
            typeof(InterfaceTypeDefinitionNode),
            new List<IMergeSchemaNodeOperation> { new ApplySourceDirectiveToInterfaceDefinitionOperation() }
        },
        {
            typeof(FieldDefinitionNode),
            new List<IMergeSchemaNodeOperation> { new MergeFieldDefinitionOperation(), new ApplySourceDirectiveToFieldDefinitionOperation() }
        }
    };

    public void Apply<TParentSourceNode, TParentTargetNode, TSourceSyntaxNode>(
        TParentSourceNode source, TParentTargetNode destination,
        Func<TParentSourceNode, TSourceSyntaxNode> sourceAccessor,
        MergeOperationContext context)
        where TParentSourceNode : ISchemaNode
        where TParentTargetNode : ISchemaNode
        where TSourceSyntaxNode : ISyntaxNode
    {
        TSourceSyntaxNode sourceNode = sourceAccessor.Invoke(source);

        if (!source.Database.TryGet(source, sourceNode, out ISchemaNode? sourceSchemaNode))
            throw new InvalidOperationException();

        ISchemaNode destinationSchemaNode = destination.Database
            .GetOrAdd(destination, sourceNode);

        Apply(sourceSchemaNode, destinationSchemaNode, context);
    }

    public void Apply<TParentSourceNode, TParentTargetNode, TSourceSyntaxNode>(
        TParentSourceNode source,
        TParentTargetNode destination,
        Func<TParentSourceNode, IEnumerable<TSourceSyntaxNode>> sourceAccessor,
        MergeOperationContext context)
        where TParentSourceNode : ISchemaNode
        where TParentTargetNode : ISchemaNode
        where TSourceSyntaxNode : ISyntaxNode
    {
        IEnumerable<TSourceSyntaxNode> sourceSyntaxNodes = sourceAccessor.Invoke(source);

        foreach (TSourceSyntaxNode node in sourceSyntaxNodes)
        {
            if (!source.Database.TryGet(source, node, out ISchemaNode? sourceSchemaNode))
                throw new InvalidOperationException();

            ISchemaNode destinationSchemaNode = destination.Database
                .GetOrAdd(destination, node);

            Apply(sourceSchemaNode, destinationSchemaNode, context);
        }
    }

    public void Apply(ISchemaNode sourceDefinition, ISchemaNode destinationDefinition)
    {
        var context = new MergeOperationContext(this);

        Apply(sourceDefinition,
            destinationDefinition,
            context);
    }

    private static void Apply(
        ISchemaNode source,
        ISchemaNode destination,
        MergeOperationContext context)
    {
        IEnumerable<ISchemaNode> childSyntaxNodes = source.DescendentNodes();
        foreach (ISchemaNode nodeReference in childSyntaxNodes)
        {
            ISchemaNode targetDefinition = destination.Database.GetOrAdd(nodeReference);

            ICollection<IMergeSchemaNodeOperation> operations = GetOperations(nodeReference);
            foreach (IMergeSchemaNodeOperation operation in operations)
                operation.Apply(nodeReference, targetDefinition, context);
        }
    }

    private static ICollection<IMergeSchemaNodeOperation> GetOperations(ISchemaNode source)
    {
        ICollection<IMergeSchemaNodeOperation> definitionOperations =
            _operations.TryGetValue(source.GetType(),
                out ICollection<IMergeSchemaNodeOperation>? operations)
                ? operations
                : new List<IMergeSchemaNodeOperation>(0);

        ICollection<IMergeSchemaNodeOperation> syntaxNodeOperations =
            _operations.TryGetValue(source.Definition.GetType(),
                out ICollection<IMergeSchemaNodeOperation>? nodeOperations)
                ? nodeOperations
                : new List<IMergeSchemaNodeOperation>(0);

        return definitionOperations.Concat(syntaxNodeOperations)
            .ToList();
    }
}
