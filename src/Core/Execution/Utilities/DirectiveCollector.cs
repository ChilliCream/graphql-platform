using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

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
                        CollectDirectives(fieldSelection, ot, field));
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
                            CollectDirectives(fieldSelection, ot2, field));
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
            ObjectType type,
            ObjectField field)
        {
            HashSet<string> processed = new HashSet<string>();
            List<IDirective> directives = new List<IDirective>();

            CollectSelectionDirectives(processed, directives, fieldSelection);
            // CollectFieldDirectives(processed, directives, field.Arguments);
            CollectFieldDirectives(processed, directives, field);
            CollectFieldDirectives(processed, directives,
                field.InterfaceFields);
            CollectTypeDirectives(processed, directives, type);
            CollectTypeDirectives(processed, directives,
                type.Interfaces.Values);

            return directives.AsReadOnly();
        }

        private void CollectSelectionDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            FieldNode fieldSelection)
        {
            foreach (IDirective directive in
                GetFieldSelectionDirectives(fieldSelection))
            {
                if (processed.Add(directive.Name))
                {
                    directives.Add(directive);
                }
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

        private void CollectFieldDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            IEnumerable<IField> fields)
        {
            foreach (IField field in fields)
            {
                CollectFieldDirectives(processed, directives, field);
            }
        }

        private void CollectFieldDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            IField field)
        {
            if (field is Types.IHasDirectives d)
            {
                foreach (IDirective directive in
                    d.Directives.Where(t => t.IsExecutable))
                {
                    if (processed.Add(directive.Name))
                    {
                        directives.Add(directive);
                    }
                }
            }
        }

        private void CollectTypeDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            IEnumerable<IOutputType> types)
        {
            foreach (IOutputType type in types)
            {
                CollectTypeDirectives(processed, directives, type);
            }
        }

        private void CollectTypeDirectives(
            HashSet<string> processed,
            List<IDirective> directives,
            IOutputType type)
        {
            if (type is Types.IHasDirectives d)
            {
                foreach (IDirective directive in
                    d.Directives.Where(t => t.IsExecutable))
                {
                    if (processed.Add(directive.Name))
                    {
                        directives.Add(directive);
                    }
                }
            }
        }
    }
}
