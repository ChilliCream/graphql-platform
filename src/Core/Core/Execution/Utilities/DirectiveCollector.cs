using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Validation;

namespace HotChocolate.Execution
{
    internal sealed class DirectiveCollector
        : Validation.QueryVisitor
    {
        private readonly IDictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>> _directiveLookup =
            new Dictionary<ObjectType, IDictionary<FieldNode, IReadOnlyCollection<IDirective>>>();

        public DirectiveCollector(ISchema schema)
            : base(schema)
        {
        }

        public DirectiveLookup CreateLookup()
        {
            return new DirectiveLookup(_directiveLookup);
        }

        protected override void VisitField(
            FieldNode fieldSelection,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            CollectDirectives(fieldSelection, type);
            base.VisitField(fieldSelection, type, path);
        }

        public void CollectDirectives(
            FieldNode fieldSelection,
            IType type)
        {
            if (type is ObjectType ot)
            {
                if (ot.Fields.TryGetField(
                    fieldSelection.Name.Value,
                    out ObjectField field))
                {
                    UpdateDirectiveLookup(ot, fieldSelection,
                        CollectDirectives(fieldSelection, field));
                }
            }
            else if (type.IsAbstractType() && type is INamedType nt)
            {
                foreach (ObjectType ot2 in Schema.GetPossibleTypes(nt))
                {
                    if (ot2.Fields.TryGetField(
                        fieldSelection.Name.Value,
                        out ObjectField field))
                    {
                        UpdateDirectiveLookup(ot2, fieldSelection,
                            CollectDirectives(fieldSelection, field));
                    }
                }
            }
        }

        private void UpdateDirectiveLookup(
            ObjectType type,
            FieldNode fieldSelection,
            IReadOnlyCollection<IDirective> directives)
        {
            if (!_directiveLookup.TryGetValue(
                type, out var selectionToDirectives))
            {
                selectionToDirectives =
                    new Dictionary<FieldNode, IReadOnlyCollection<IDirective>>();
                _directiveLookup[type] = selectionToDirectives;
            }
            selectionToDirectives[fieldSelection] = directives;
        }

        private IReadOnlyCollection<IDirective> CollectDirectives(
            FieldNode fieldSelection,
            ObjectField field)
        {
            HashSet<string> processed = new HashSet<string>();
            List<IDirective> directives = new List<IDirective>();

            CollectTypeSystemDirectives(processed, directives, field);
            CollectQueryDirectives(processed, directives, fieldSelection);

            return directives.AsReadOnly();
        }

        private void CollectQueryDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            FieldNode fieldSelection)
        {
            foreach (IDirective directive in
                GetFieldSelectionDirectives(fieldSelection))
            {
                if (!processed.Add(directive.Name))
                {
                    directives.Remove(directive);
                }
                directives.Add(directive);
            }
        }

        private IEnumerable<IDirective> GetFieldSelectionDirectives(
            FieldNode fieldSelection)
        {
            foreach (DirectiveNode directive in fieldSelection.Directives)
            {
                if (Schema.TryGetDirectiveType(directive.Name.Value,
                    out DirectiveType directiveType))
                {
                    if (directiveType.IsExecutable)
                    {
                        yield return new Directive(
                            directiveType, directive, fieldSelection);
                    }
                }
            }
        }

        private static void CollectTypeSystemDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            ObjectField field)
        {
            foreach (IDirective directive in field.ExecutableDirectives)
            {
                if (!processed.Add(directive.Name))
                {
                    directives.Remove(directive);
                }
                directives.Add(directive);
            }
        }
    }

    internal sealed class CollectDirectivesVisitor
        : QuerySyntaxWalker<CollectDirectivesVisitor.Context>
    {
        private static CollectDirectivesVisitor _visitor =
            new CollectDirectivesVisitor();
        protected override bool VisitFragmentDefinitions => false;

        public static ILookup<FieldNode, IDirective> CollectDirectives(
            DocumentNode document,
            ISchema schema)
        {
            var context = Context.New(schema);
            _visitor.Visit(document, context);
            return context.Directives.ToLookup(t => t.Value, t => t.Key);
        }

        protected override void VisitDocument(
            DocumentNode node,
            CollectDirectivesVisitor.Context context)
        {
            foreach (var fragment in node.Definitions
                .OfType<FragmentDefinitionNode>()
                .Where(t => t.Name?.Value != null))
            {
                context.Fragments[fragment.Name.Value] = fragment;
            }

            foreach (var operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                if (operation.TryGetOperationType(
                    context.Schema,
                    out ObjectType objectType))
                {
                    VisitOperationDefinition(
                        operation,
                        context);
                }
            }
        }

        protected override void VisitField(
            FieldNode node,
            CollectDirectivesVisitor.Context context)
        {
            if (context.  !context.Directives.ContainsKey(node))
            {
                foreach (DirectiveNode directive in node.Directives)
                {

                }





                base.VisitField(node, context);
            }
        }

        protected override void VisitFragmentDefinition(
             FragmentDefinitionNode node,
             Context context)
        {
            Context newContext = context;

            if (newContext.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext.SetTypeContext(type);
            }

            base.VisitFragmentDefinition(node, newContext);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            Context context)
        {
            Context newContext = context;

            if (newContext.Schema.TryGetType(
                node.TypeCondition.Name.Value,
                out IComplexOutputType type))
            {
                newContext = newContext.SetTypeContext(type);
            }

            base.VisitInlineFragment(node, newContext);
        }

        internal sealed class Context
        {
            private Context(ISchema schema)
            {
                Schema = schema
                    ?? throw new ArgumentNullException(nameof(schema));
                FragmentPath = ImmutableHashSet<string>.Empty;
                Fragments = new Dictionary<string, FragmentDefinitionNode>();
                Directives = new Dictionary<FieldNode, IList<IDirective>>();
            }

            private Context(
                ISchema schema,
                ImmutableHashSet<string> fragmentPath,
                IDictionary<string, FragmentDefinitionNode> fragments,
                Dictionary<FieldNode, IList<IDirective>> directives)
            {
                Schema = schema;
                FragmentPath = fragmentPath;
                Fragments = fragments;
                Directives = directives;
            }

            public ISchema Schema { get; }

            public INamedOutputType TypeContext { get; }

            public ImmutableHashSet<string> FragmentPath { get; }

            public IDictionary<string, FragmentDefinitionNode> Fragments
            { get; }

            public IDictionary<FieldNode, IList<IDirective>> Directives
            { get; }

            public virtual Context SetTypeContext(
                INamedOutputType typeContext)
            {
                var newContext = new Context(this);
                newContext.TypeContext = typeContext;
                return newContext;
            }

            public Context AddFragment(FragmentDefinitionNode fragment)
            {
                return new Context(
                    _schema,
                    FragmentPath.Add(fragment.Name.Value),
                    Fragments,
                    Directives);
            }

            public static Context New(ISchema schema) => new Context(schema);
        }

        private class DirectiveLookup
            : ILookup<FieldNode, IDirective>
        {
            private readonly IDictionary<FieldNode, IList<IDirective>> _dirs;

            public DirectiveLookup(
                IDictionary<FieldNode, IList<IDirective>> directives)
            {
                _dirs = directives
                    ?? throw new ArgumentNullException(nameof(directives));
            }

            public IEnumerable<IDirective> this[FieldNode key]
            {
                get
                {
                    if (_dirs.TryGetValue(key, out IList<IDirective> dirs))
                    {
                        return dirs;
                    }
                    return Array.Empty<IDirective>();
                }
            }

            public int Count => _dirs.Count;

            public bool Contains(FieldNode key) => _dirs.ContainsKey(key);

            public IEnumerator<IGrouping<FieldNode, IDirective>> GetEnumerator()
                => _dirs.Select(t => new Grouping(t.Key, t.Value))
                    .GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class Grouping
            : IGrouping<FieldNode, IDirective>
        {
            private readonly IEnumerable<IDirective> _directives;

            public Grouping(FieldNode field, IEnumerable<IDirective> directives)
            {
                Key = field;
                _directives = directives;
            }

            public FieldNode Key { get; }

            public IEnumerator<IDirective> GetEnumerator() =>
                _directives.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
