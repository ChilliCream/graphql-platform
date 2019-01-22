using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching
{
    public class BranchQueryRewriter
    {

    }

    public class ExtractFieldQuerySyntaxRewriter
        : QuerySyntaxRewriter<ExtractFieldQuerySyntaxRewriter.Context>
    {
        private ISchema _schema;

        protected override bool VisitFragmentDefinitions => false;

        public ExtractedField ExtractField(
            DocumentNode document,
            OperationDefinitionNode operation,
            FieldNode field,
            INamedOutputType declaringType)
        {
            if (document == null)
            {
                throw new System.ArgumentNullException(nameof(document));
            }

            if (field == null)
            {
                throw new System.ArgumentNullException(nameof(field));
            }

            if (declaringType == null)
            {
                throw new System.ArgumentNullException(nameof(declaringType));
            }

            var context = Context.New(declaringType, document);

            FieldNode rewrittenField = RewriteField(field, context);

            return new ExtractedField(
                rewrittenField,
                context.Variables.Values.ToList(),
                context.Fragments.Values.ToList());
        }

        protected override FieldNode RewriteField(
            FieldNode node, Context context)
        {
            FieldNode current = node;

            if (context.TypeContext is IComplexOutputType type
                && type.Fields.TryGetField(node.Name.Value,
                    out IOutputField field))
            {
                if (node.Alias != null)
                {
                    current = Rewrite(current, node.Alias, context,
                        RewriteName, current.WithAlias);
                }

                current = Rewrite(current, node.Name, context,
                    RewriteName, current.WithName);

                current = Rewrite(current, node.Arguments, context,
                    (p, c) => RewriteMany(p, c, RewriteArgument),
                    current.WithArguments);

                current = Rewrite(current, node.Directives, context,
                    (p, c) => RewriteMany(p, c, RewriteDirective),
                    current.WithDirectives);


                if (node.SelectionSet != null
                    && field.Type.NamedType() is INamedOutputType n)
                {
                    current = Rewrite
                    (
                        current,
                        node.SelectionSet,
                        context.SetTypeContext(n),
                        RewriteSelectionSet,
                        current.WithSelectionSet
                    );
                }
            }

            return current;
        }

        protected override SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node,
            Context context)
        {
            SelectionSetNode current = node;

            var selections = new List<ISelectionNode>(current.Selections);

            RemoveDelegationFields(node, context, selections);
            AddTypeNameField(selections);

            current = current.WithSelections(selections);

            return base.RewriteSelectionSet(current, context);
        }

        private void RemoveDelegationFields(
            SelectionSetNode node,
            Context context,
            ICollection<ISelectionNode> selections)
        {
            if (context.TypeContext is IComplexOutputType type)
            {
                foreach (FieldNode selection in node.Selections
                    .OfType<FieldNode>())
                {
                    if (type.Fields.TryGetField(selection.Name.Value,
                        out IOutputField field)
                        && field.Directives.Contains(DirectiveNames.Delegate))
                    {
                        selections.Remove(selection);
                    }
                }
            }
        }

        private void AddTypeNameField(ICollection<ISelectionNode> selections)
        {
            selections.Add(new FieldNode
            (
                null,
                new NameNode("__typename"),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null
            ));
        }

        protected override VariableNode RewriteVariable(
            VariableNode node,
            Context context)
        {
            context.Operation.VariableDefinitions


            return base.RewriteVariable(node, context);
        }

        protected override FragmentSpreadNode RewriteFragmentSpread(
            FragmentSpreadNode node,
            Context context)
        {
            string name = node.Name.Value;
            if (!context.Fragments.TryGetValue(name,
                out FragmentDefinitionNode fragment))
            {
                fragment = context.Document.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .FirstOrDefault(t => t.Name.Value.EqualsOrdinal(name));
                fragment = RewriteFragmentDefinition(fragment, context);
                context.Fragments[name] = fragment;
            }

            return base.RewriteFragmentSpread(node, context);
        }

        protected override FragmentDefinitionNode RewriteFragmentDefinition(
            FragmentDefinitionNode node,
            Context context)
        {
            Context newContext = context;

            if (newContext.FragmentPath.Contains(node.Name.Value))
            {
                return node;
            }

            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext
                    .AddFragment(node.Name.Value)
                    .SetTypeContext(type);
            }

            return base.RewriteFragmentDefinition(node, newContext);
        }

        protected override InlineFragmentNode RewriteInlineFragment(
            InlineFragmentNode node,
            Context context)
        {
            Context newContext = context;

            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext.SetTypeContext(type);
            }

            return base.RewriteInlineFragment(node, newContext);
        }

        public class Context
        {
            private Context(
                INamedOutputType typeContext,
                DocumentNode document,
                OperationDefinitionNode operation)
            {
                Variables = new Dictionary<string, VariableDefinitionNode>();
                Document = document;
                Operation = operation;
                TypeContext = typeContext;
                Fragments = new Dictionary<string, FragmentDefinitionNode>();
                FragmentPath = ImmutableHashSet<string>.Empty;
            }

            private Context(Context context, INamedOutputType typeContext)
            {
                Variables = context.Variables;
                Document = context.Document;
                Operation = context.Operation;
                TypeContext = typeContext;
                Fragments = context.Fragments;
                FragmentPath = context.FragmentPath;
            }

            private Context(
                Context context,
                ImmutableHashSet<string> fragmentPath)
            {
                Variables = context.Variables;
                Document = context.Document;
                Operation = context.Operation;
                TypeContext = context.TypeContext;
                Fragments = context.Fragments;
                FragmentPath = fragmentPath;
            }

            public DocumentNode Document { get; }

            public OperationDefinitionNode Operation { get; }

            public IDictionary<string, VariableDefinitionNode> Variables
            { get; }

            public INamedOutputType TypeContext { get; protected set; }

            public ImmutableHashSet<string> FragmentPath { get; }

            public IDictionary<string, FragmentDefinitionNode> Fragments
            { get; }

            public Context SetTypeContext(INamedOutputType type)
            {
                return new Context(this, type);
            }

            public Context AddFragment(string fragmentName)
            {
                return new Context(this, FragmentPath.Add(fragmentName));
            }

            public static Context New(
                INamedOutputType typeContext,
                DocumentNode document)
            {
                return new Context(typeContext, document);
            }
        }
    }

    public class ExtractedField
    {

        public ExtractedField(
            FieldNode field,
            IReadOnlyList<VariableDefinitionNode> variables,
            IReadOnlyList<FragmentDefinitionNode> fragments)
        {
            Field = field
                ?? throw new ArgumentNullException(nameof(field));
            Variables = variables
                ?? throw new ArgumentNullException(nameof(variables));
            Fragments = fragments
                ?? throw new ArgumentNullException(nameof(fragments));
        }

        public FieldNode Field { get; }
        public IReadOnlyList<VariableDefinitionNode> Variables { get; }
        public IReadOnlyList<FragmentDefinitionNode> Fragments { get; }
    }
}
