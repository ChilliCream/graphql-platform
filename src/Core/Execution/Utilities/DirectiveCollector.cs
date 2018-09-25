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
        private readonly Dictionary<FieldNode, List<IDirective>> _directives =
            new Dictionary<FieldNode, List<IDirective>>();

        public DirectiveCollector2(ISchema schema)
            : base(schema)
        {
        }



        protected override void VisitField(
            FieldNode fieldSelection,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is IComplexOutputType complexType
                && complexType.Fields.ContainsField(fieldSelection.Name.Value))
            {
                IOutputField field =
                    complexType.Fields[fieldSelection.Name.Value];

                CollectDirectives(complexType, field, fieldSelection);


            }

            base.VisitField(fieldSelection, type, path);
        }


        public IReadOnlyCollection<IDirective> CollectDirectives(
            IOutputType objectType,
            IOutputField field,
            FieldNode fieldSelection)
        {
            HashSet<string> processed = new HashSet<string>();
            List<IDirective> directives = new List<IDirective>();

            CollectSelectionDirectives(processed, directives, fieldSelection);
            CollectFieldDirectives(processed, directives, field);
            CollectTypeDirectives(processed, directives, objectType);

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
                foreach (IDirective directive in d.Directives)
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
                foreach (IDirective directive in d.Directives)
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
