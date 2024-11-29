using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class EntityIdRewriter : SyntaxRewriter<EntityIdRewriter.Context>
{
    protected override OperationDefinitionNode RewriteOperationDefinition(
        OperationDefinitionNode node,
        Context context)
    {
        context.Nodes.Push(node);
        context.Types.Push(context.Schema.GetOperationType(node.Operation)!);
        node = base.RewriteOperationDefinition(node, context)!;
        context.Types.Pop();
        context.Nodes.Pop();
        return node;
    }

    protected override FieldNode RewriteField(
        FieldNode node,
        Context context)
    {
        if(node.Name.Value.Equals(WellKnownNames.TypeName))
        {
            return node;
        }

        var field = ((IComplexOutputType)context.Types.Peek()).Fields[node.Name.Value];

        if(field.Type.NamedType().IsLeafType())
        {
            return node;
        }

        context.Nodes.Push(node);
        context.Types.Push(field.Type.NamedType());
        node = base.RewriteField(node, context)!;
        context.Types.Pop();
        context.Nodes.Pop();
        return node;
    }

    protected override InlineFragmentNode RewriteInlineFragment(
        InlineFragmentNode node,
        Context context)
    {
        context.Nodes.Push(node);
        context.Types.Push(node.TypeCondition is null
            ? context.Types.Peek()
            : context.Schema.GetType<INamedType>(node.TypeCondition.Name.Value));
        node = base.RewriteInlineFragment(node, context)!;
        context.Types.Pop();
        context.Nodes.Pop();
        return node;
    }

    protected override FragmentDefinitionNode RewriteFragmentDefinition(
        FragmentDefinitionNode node,
        Context context)
    {
        context.Nodes.Push(node);
        context.Types.Push(context.Schema.GetType<INamedType>(node.TypeCondition.Name.Value));
        node = base.RewriteFragmentDefinition(node, context)!;
        context.Types.Pop();
        context.Nodes.Pop();
        return node;
    }

    protected override SelectionSetNode RewriteSelectionSet(
        SelectionSetNode node,
        Context context)
    {
        var current = base.RewriteSelectionSet(node, context)!;

        if (context.Nodes.Peek() is FieldNode or OperationDefinitionNode)
        {
            var selections = current.Selections.ToList();

            foreach (var objectType in
                     context.Schema.GetPossibleTypes(context.Types.Peek()))
            {
                if (objectType.IsEntity())
                {
                    var entityDefinition = objectType.GetEntityDefinition();
                    List<ISelectionNode> fields = [];

                    foreach (var selection in entityDefinition.Selections)
                    {
                        fields.Add(selection);
                    }

                    selections.Add(new InlineFragmentNode(
                        null,
                        new NamedTypeNode(objectType.Name),
                        new List<DirectiveNode>(),
                        new SelectionSetNode(fields)));
                }
            }

            current = current.WithSelections(selections);
        }

        return current;
    }

    public static DocumentNode Rewrite(DocumentNode document, ISchema schema)
    {
        var rewriter = new EntityIdRewriter();
        return rewriter.RewriteDocument(document, new Context(schema))!;
    }

    public class Context(ISchema schema)
    {
        public ISchema Schema { get; } = schema;

        public Stack<INamedType> Types { get; } = new();

        public Stack<ISyntaxNode> Nodes { get; } = new();
    }
}
