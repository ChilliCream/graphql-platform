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
}
