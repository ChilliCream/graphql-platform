using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class EntityIdRewriter
        : QuerySyntaxRewriter<EntityIdRewriter.Context>
    {
        private const string _typeName = "__typename";
        private readonly Dictionary<string, INamedType> _fragmentTypes = new();

        protected override DocumentNode RewriteDocument(DocumentNode node, Context context)
        {
            foreach (var fragment in node.Definitions.OfType<FragmentDefinitionNode>())
            {
                _fragmentTypes.Add(
                    fragment.Name.Value,
                    context.Schema.GetType<INamedType>(fragment.TypeCondition.Name.Value));
            }

            return base.RewriteDocument(node, context);
        }

        protected override OperationDefinitionNode RewriteOperationDefinition(
            OperationDefinitionNode node,
            Context context)
        {
            context.Nodes.Push(node);
            context.Types.Push(context.Schema.GetOperationType(node.Operation));
            node = base.RewriteOperationDefinition(node, context);
            context.Types.Pop();
            context.Nodes.Pop();
            return node;
        }

        protected override FieldNode RewriteField(
            FieldNode node,
            Context context)
        {
            if (node.Name.Value is _typeName)
            {
                return node;
            }

            IOutputField field = ((IComplexOutputType)context.Types.Peek()).Fields[node.Name.Value];
            context.Nodes.Push(node);
            context.Types.Push(field.Type.NamedType());
            node = base.RewriteField(node, context);
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
            node = base.RewriteInlineFragment(node, context);
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
            node = base.RewriteFragmentDefinition(node, context);
            context.Types.Pop();
            context.Nodes.Pop();
            return node;
        }

        protected override SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node,
            Context context)
        {
            SelectionSetNode current = base.RewriteSelectionSet(node, context);

            if (NeedsEntityIdFields(context))
            {
                List<ISelectionNode> selections = current.Selections.ToList();

                foreach (var objectType in context.Schema.GetPossibleTypes(context.Types.Peek()))
                {
                    SelectionSetNode entityDefinition = objectType.GetEntityDefinition();
                    List<ISelectionNode> fields = new();

                    foreach (var selection in entityDefinition.Selections)
                    {
                        fields.Add(selection);
                    }

                    selections.Add(new InlineFragmentNode(
                        null,
                        new NamedTypeNode(objectType.Name.Value),
                        new List<DirectiveNode>(),
                        new SelectionSetNode(fields)));
                }

                current = current.WithSelections(selections);
            }

            return current;
        }

        private bool NeedsEntityIdFields(Context context)
        {
            return context.Nodes.Peek() is FieldNode or OperationDefinitionNode &&
                context.Schema.GetPossibleTypes(context.Types.Peek()).Any(t => t.IsEntity());
        }

        public static DocumentNode Rewrite(DocumentNode document, ISchema schema)
        {
            var rewriter = new EntityIdRewriter();
            return rewriter.RewriteDocument(document, new Context(schema));
        }

        public class Context
        {
            public Context(ISchema schema)
            {
                Schema = schema;
            }

            public ISchema Schema { get; }

            public Stack<INamedType> Types { get; } = new();

            public Stack<ISyntaxNode> Nodes { get; } = new();
        }
    }
}
