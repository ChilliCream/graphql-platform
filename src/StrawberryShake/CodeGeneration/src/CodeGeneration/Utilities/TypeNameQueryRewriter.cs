using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class TypeNameQueryRewriter
        : QuerySyntaxRewriter<TypeNameQueryRewriter.Context>
    {
        private const string _typeName = "__typename";

        private static readonly FieldNode _typeNameField = new(
            null,
            new NameNode(_typeName),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

        protected override OperationDefinitionNode RewriteOperationDefinition(
            OperationDefinitionNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteOperationDefinition(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override FieldNode RewriteField(
            FieldNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteField(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override InlineFragmentNode RewriteInlineFragment(
            InlineFragmentNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteInlineFragment(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override FragmentDefinitionNode RewriteFragmentDefinition(
            FragmentDefinitionNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteFragmentDefinition(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node,
            Context context)
        {
            SelectionSetNode current = base.RewriteSelectionSet(node, context);

            if (context.Nodes.Peek() is FieldNode &&
                !current.Selections.OfType<FieldNode>().Any(
                    t => t.Alias is null && t.Name.Value.EqualsOrdinal(_typeName)))
            {
                List<ISelectionNode> selections = current.Selections.ToList();
                selections.Insert(0, _typeNameField);
                current = current.WithSelections(selections);
            }

            return current;
        }

        public static DocumentNode Rewrite(DocumentNode document)
        {
            var rewriter = new TypeNameQueryRewriter();
            return rewriter.RewriteDocument(document, new());
        }

        public class Context
        {
            public Stack<ISyntaxNode> Nodes { get; } = new();
        }
    }

    internal sealed class EntityIdRewriter
        : QuerySyntaxRewriter<EntityIdRewriter.Context>
    {
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
            SelectionSetNode node, Context context)
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

    internal sealed class RemoveClientDirectivesRewriter
        : QuerySyntaxRewriter<object?>
    {
        private const string _returns = "returns";

        protected override FieldNode RewriteField(FieldNode node, object? context)
        {
            FieldNode current = node;

            if (current.Directives.Any(t => t.Name.Value.EqualsOrdinal(_returns)))
            {
                var directiveNodes = current.Directives.ToList();
                directiveNodes.RemoveAll(t => t.Name.Value.EqualsOrdinal(_returns));
                current = current.WithDirectives(directiveNodes);
            }

            return base.RewriteField(current, context);
        }

        public static DocumentNode Rewrite(DocumentNode document)
        {
            var rewriter = new RemoveClientDirectivesRewriter();
            return rewriter.RewriteDocument(document, null);
        }
    }

    public static class QueryDocumentRewriter
    {
        public static DocumentNode Rewrite(DocumentNode document, ISchema schema)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            DocumentNode current = document;
            current = EntityIdRewriter.Rewrite(current, schema);
            current = TypeNameQueryRewriter.Rewrite(current);
            return RemoveClientDirectivesRewriter.Rewrite(current);
        }
    }
}
