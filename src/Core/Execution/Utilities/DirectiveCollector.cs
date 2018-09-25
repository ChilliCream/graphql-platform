using System;
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
    {
        private readonly ISchema _schema;

        public DirectiveCollector(ISchema schema)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
        }

        public IReadOnlyCollection<IDirective> CollectDirectives(
            ObjectType objectType,
            ObjectField field,
            FieldNode fieldSelection,
            DirectiveScope scope)
        {
            HashSet<string> processed = new HashSet<string>();
            Stack<IDirective> directives = new Stack<IDirective>();

            CollectSelectionDirectives(processed, directives, fieldSelection);

            if (scope == DirectiveScope.All)
            {
                CollectFieldDirectives(processed, directives, field);
                CollectFieldDirectives(processed, directives,
                    field.InterfaceFields);

                CollectTypeDirectives(processed, directives, objectType);
                CollectTypeDirectives(processed, directives,
                    objectType.Interfaces.Values);
            }

            return directives;
        }

        private void CollectSelectionDirectives(
            HashSet<string> processed,
            Stack<IDirective> directives,
            FieldNode fieldSelection)
        {
            foreach (IDirective directive in
                GetFieldSelectionDirectives(fieldSelection))
            {
                if (processed.Add(directive.Name))
                {
                    directives.Push(directive);
                }
            }
        }

        private IEnumerable<IDirective> GetFieldSelectionDirectives(
            FieldNode fieldSelection)
        {
            foreach (DirectiveNode directive in fieldSelection.Directives)
            {
                if (_schema.TryGetDirectiveType(directive.Name.Value,
                    out DirectiveType directiveType))
                {
                    yield return new Directive(directiveType, directive);
                }
            }
        }

        private void CollectFieldDirectives(
            HashSet<string> processed,
            Stack<IDirective> directives,
            IEnumerable<IField> fields)
        {
            foreach (IField field in fields)
            {
                CollectFieldDirectives(processed, directives, field);
            }
        }

        private void CollectFieldDirectives(
            HashSet<string> processed,
            Stack<IDirective> directives,
            IField field)
        {
            if (field is Types.IHasDirectives d)
            {
                foreach (IDirective directive in d.Directives)
                {
                    if (processed.Add(directive.Name))
                    {
                        directives.Push(directive);
                    }
                }
            }
        }

        private void CollectTypeDirectives(
            HashSet<string> processed,
            Stack<IDirective> directives,
            IEnumerable<TypeBase> types)
        {
            foreach (TypeBase type in types)
            {
                CollectTypeDirectives(processed, directives, type);
            }
        }

        private void CollectTypeDirectives(
            HashSet<string> processed,
            Stack<IDirective> directives,
            TypeBase type)
        {
            if (type is Types.IHasDirectives d)
            {
                foreach (IDirective directive in d.Directives)
                {
                    if (processed.Add(directive.Name))
                    {
                        directives.Push(directive);
                    }
                }
            }
        }
    }

    internal sealed class DirectiveCollector2
        : Validation.QueryVisitor
    {
        private readonly Dictionary<DirectiveLookup.SelectionKey, List<IDirective>> _directives =
            new Dictionary<DirectiveLookup.SelectionKey, List<IDirective>>();

        public DirectiveCollector2(ISchema schema)
            : base(schema)
        {
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
            if (type.IsAbstractType() && type is INamedType nt)
            {
                foreach (ObjectType objectType in Schema.GetPossibleTypes(nt))
                {
                    var key = new DirectiveLookup.SelectionKey(
                        fieldSelection, objectType);

                    if (objectType.Fields.TryGetField(
                        fieldSelection.Name.Value,
                        out ObjectField field))
                    {
                        _directives[key] = CollectDirectives(
                            fieldSelection, objectType, field);
                    }
                }
            }
            else if (type is ObjectType ot)
            {
                var key = new DirectiveLookup.SelectionKey(
                    fieldSelection, ot);

                if (ot.Fields.TryGetField(
                    fieldSelection.Name.Value,
                    out ObjectField field))
                {
                    _directives[key] = CollectDirectives(
                        fieldSelection, ot, field);
                }
            }
        }

        public List<IDirective> CollectDirectives(
            FieldNode fieldSelection,
            ObjectType type,
            ObjectField field)
        {
            HashSet<string> processed = new HashSet<string>();
            List<IDirective> directives = new List<IDirective>();

            CollectSelectionDirectives(processed, directives, fieldSelection);
            CollectFieldDirectives(processed, directives, field);
            CollectTypeDirectives(processed, directives, type);

            return directives;
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
                    yield return new Directive(directiveType, directive);
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

    internal class DirectiveLookup
    {

        public IReadOnlyCollection<IDirective> GetDirectives(
            ObjectType type, FieldNode fieldSelection)
        {

        }

        internal sealed class SelectionKey
        {
            public SelectionKey(FieldNode fieldSelection, ObjectType type)
            {
                FieldSelection = fieldSelection
                    ?? throw new ArgumentNullException(nameof(fieldSelection));
                Type = type
                    ?? throw new ArgumentNullException(nameof(type));
            }

            public FieldNode FieldSelection { get; }

            public ObjectType Type { get; }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }

                if (ReferenceEquals(obj, this))
                {
                    return true;
                }

                if (obj is SelectionKey k)
                {
                    return ReferenceEquals(FieldSelection, k.FieldSelection)
                        && ReferenceEquals(Type, k.Type);
                }

                return false;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (397 * FieldSelection.GetHashCode())
                        ^ Type.GetHashCode();
                }
            }
        }
    }
}
