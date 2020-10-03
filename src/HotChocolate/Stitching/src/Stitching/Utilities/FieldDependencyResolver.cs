using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Utilities
{
    public class FieldDependencyResolver
        : QuerySyntaxWalker<FieldDependencyResolver.Context>
    {
        private readonly ISchema _schema;

        public FieldDependencyResolver(ISchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        protected override bool VisitFragmentDefinitions => false;

        public ISet<FieldDependency> GetFieldDependencies(
            DocumentNode document,
            FieldNode field,
            INamedOutputType declaringType)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            var context = Context.New(declaringType, GetFragments(document));

            VisitSelectionSet(field.SelectionSet, context);

            return context.Dependencies;
        }

        public ISet<FieldDependency> GetFieldDependencies(
            DocumentNode document,
            SelectionSetNode selectionSet,
            INamedOutputType declaringType)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            var context = Context.New(declaringType, GetFragments(document));

            VisitSelectionSet(selectionSet, context);

            return context.Dependencies;
        }

        private static IDictionary<string, FragmentDefinitionNode> GetFragments(
            DocumentNode document)
        {
            Dictionary<string, FragmentDefinitionNode> fragments =
                new Dictionary<string, FragmentDefinitionNode>();

            foreach (FragmentDefinitionNode fragment in
                document.Definitions.OfType<FragmentDefinitionNode>())
            {
                if (!string.IsNullOrEmpty(fragment.Name?.Value))
                {
                    fragments[fragment.Name.Value] = fragment;
                }
            }

            return fragments;
        }

        protected override void VisitField(FieldNode node, Context context)
        {
            if (context.TypeContext is IComplexOutputType type
                && type.Fields.TryGetField(node.Name.Value,
                    out IOutputField field))
            {
                CollectDelegationDependencies(context, type, field);
                CollectComputeDependencies(context, type, field);
            }
        }

        private static void CollectDelegationDependencies(
            Context context,
            Types.IHasName type,
            IOutputField field)
        {
            IDirective directive = field.Directives[DirectiveNames.Delegate]
                .FirstOrDefault();

            if (directive != null)
            {
                CollectFieldNames(
                    directive.ToObject<DelegateDirective>(),
                    type,
                    context.Dependencies);
            }
        }

        private static void CollectComputeDependencies(
            Context context,
            IComplexOutputType type,
            IOutputField field)
        {
            IDirective directive = field.Directives[DirectiveNames.Computed]
                .FirstOrDefault();

            if (directive != null)
            {
                NameString[] dependantOn = directive
                    .ToObject<ComputedDirective>()
                    .DependantOn;

                if (dependantOn != null)
                {
                    foreach (string fieldName in dependantOn)
                    {
                        if (type.Fields.TryGetField(
                            fieldName,
                            out IOutputField dependency))
                        {
                            context.Dependencies.Add(new FieldDependency(
                                type.Name, dependency.Name));
                        }
                    }
                }
            }
        }

        private static void CollectFieldNames(
            DelegateDirective directive,
            Types.IHasName type,
            ISet<FieldDependency> dependencies)
        {
            IImmutableStack<SelectionPathComponent> path =
                SelectionPathParser.Parse(directive.Path);

            foreach (SelectionPathComponent component in path)
            {
                foreach (string fieldName in component.Arguments
                    .Select(t => t.Value)
                    .OfType<ScopedVariableNode>()
                    .Where(t => ScopeNames.Fields.Equals(t.Scope.Value))
                    .Select(t => t.Name.Value))
                {
                    dependencies.Add(new FieldDependency(type.Name, fieldName));
                }
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode node,
            Context context)
        {
            base.VisitFragmentSpread(node, context);

            if (context.Fragments.TryGetValue(node.Name.Value,
                out FragmentDefinitionNode fragment))
            {
                VisitFragmentDefinition(fragment, context);
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            Context context)
        {
            Context newContext = context;

            if (newContext.FragmentPath.Contains(node.Name.Value))
            {
                return;
            }

            if (_schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext
                    .AddFragment(node.Name.Value)
                    .SetTypeContext(type);
            }

            base.VisitFragmentDefinition(node, newContext);
        }

        protected override void VisitInlineFragment(
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

            base.VisitInlineFragment(node, newContext);
        }

        public class Context
        {
            private Context(
                INamedOutputType typeContext,
                IDictionary<string, FragmentDefinitionNode> fragments)
            {
                Dependencies = new HashSet<FieldDependency>();
                TypeContext = typeContext;
                Fragments = fragments;
                FragmentPath = ImmutableHashSet<string>.Empty;
            }

            private Context(Context context, INamedOutputType typeContext)
            {
                Dependencies = context.Dependencies;
                TypeContext = typeContext;
                Fragments = context.Fragments;
                FragmentPath = context.FragmentPath;
            }

            private Context(
                Context context,
                ImmutableHashSet<string> fragmentPath)
            {
                Dependencies = context.Dependencies;
                TypeContext = context.TypeContext;
                Fragments = context.Fragments;
                FragmentPath = fragmentPath;
            }

            public ISet<FieldDependency> Dependencies { get; }

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
                IDictionary<string, FragmentDefinitionNode> fragments)
            {
                return new Context(typeContext, fragments);
            }
        }
    }
}
