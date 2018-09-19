using System;
using System.Collections;
using System.Collections.Generic;
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
}
