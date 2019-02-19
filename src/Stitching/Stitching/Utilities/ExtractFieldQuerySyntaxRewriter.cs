using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Utilities
{
    public class ExtractFieldQuerySyntaxRewriter
        : QuerySyntaxRewriter<ExtractFieldQuerySyntaxRewriter.Context>
    {
        private readonly ISchema _schema;
        private readonly FieldDependencyResolver _dependencyResolver;

        public ExtractFieldQuerySyntaxRewriter(ISchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _dependencyResolver = new FieldDependencyResolver(schema);
        }

        protected override bool VisitFragmentDefinitions => false;

        public ExtractedField ExtractField(
            NameString sourceSchema,
            DocumentNode document,
            OperationDefinitionNode operation,
            FieldNode field,
            INamedOutputType declaringType)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            sourceSchema.EnsureNotEmpty(nameof(sourceSchema));

            var context = Context.New(sourceSchema,
                declaringType, document, operation);

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
                SourceDirective sourceDirective =
                    field.Directives[DirectiveNames.Source]
                        .Select(t => t.ToObject<SourceDirective>())
                        .FirstOrDefault(t => context.Schema.Equals(t.Schema));

                if (sourceDirective != null)
                {
                    if (current.Alias == null)
                    {
                        current = current.WithAlias(current.Name);
                    }
                    current = current.WithName(
                        new NameNode(sourceDirective.Name));
                }

                current = Rewrite(current, node.Arguments, context,
                    (p, c) => RewriteMany(p, c, RewriteArgument),
                    current.WithArguments);

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

            ISet<FieldDependency> dependencies =
                _dependencyResolver.GetFieldDependencies(
                    context.Document, current, context.TypeContext);

            RemoveDelegationFields(node, context, selections);
            AddDependencies(context.TypeContext, selections, dependencies);
            selections.Add(CreateField(WellKnownFieldNames.TypeName));

            current = current.WithSelections(selections);

            return base.RewriteSelectionSet(current, context);
        }

        private static void RemoveDelegationFields(
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
                        && IsDelegationField(field.Directives))
                    {
                        selections.Remove(selection);
                    }
                }
            }
        }

        private static bool IsDelegationField(IDirectiveCollection directives)
        {
            return directives.Contains(DirectiveNames.Delegate)
            || directives.Contains(DirectiveNames.Computed);
        }

        private static void AddDependencies(
            IHasName typeContext,
            List<ISelectionNode> selections,
            IEnumerable<FieldDependency> dependencies)
        {

            foreach (var typeGroup in dependencies.GroupBy(t => t.TypeName))
            {
                var fields = new List<FieldNode>();

                foreach (NameString fieldName in typeGroup
                    .Select(t => t.FieldName))
                {
                    fields.Add(CreateField(fieldName));
                }

                if (typeGroup.Key.Equals(typeContext.Name))
                {
                    selections.AddRange(fields);
                }
                else
                {
                    selections.Add(new InlineFragmentNode
                    (
                        null,
                        new NamedTypeNode(null, new NameNode(typeGroup.Key)),
                        Array.Empty<DirectiveNode>(),
                        new SelectionSetNode(null, fields)
                    ));
                }
            }
        }

        private static FieldNode CreateField(string fieldName)
        {
            return new FieldNode
            (
                null,
                new NameNode(fieldName),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                null
            );
        }

        protected override VariableNode RewriteVariable(
            VariableNode node,
            Context context)
        {
            if (!context.Variables.ContainsKey(node.Name.Value))
            {
                VariableDefinitionNode variableDefinition =
                    context.Operation.VariableDefinitions
                        .FirstOrDefault(t => t.Variable.Name.Value
                            .EqualsOrdinal(node.Name.Value));
                context.Variables[node.Name.Value] = variableDefinition;
            }

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
                NameString schema,
                INamedOutputType typeContext,
                DocumentNode document,
                OperationDefinitionNode operation)
            {
                Schema = schema;
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
                Schema = context.Schema;
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
                Schema = context.Schema;
                FragmentPath = fragmentPath;
            }

            public NameString Schema { get; }

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
                NameString schema,
                INamedOutputType typeContext,
                DocumentNode document,
                OperationDefinitionNode operation)
            {
                return new Context(schema, typeContext, document, operation);
            }
        }
    }
}
