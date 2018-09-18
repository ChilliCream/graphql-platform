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
            CollectFieldDirectives(processed, directives, field);



            // 1. selection
            // 2. field
            // 3. interface fields
            // 4. objects
            // 5. interfaces
            // 6. schema

            CollectDirectives(directives, objectType.Interfaces.Values);
            CollectDirectives(directives, objectType);




            throw new NotImplementedException();
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

        private void CollectDirectives(Stack<IDirective> directives, IEnumerable<TypeBase> types)
        {
            foreach (TypeBase type in types)
            {
                CollectDirectives(directives, type);
            }
        }

        private void CollectDirectives(Stack<IDirective> directives, TypeBase type)
        {
            if (type is Types.IHasDirectives d)
            {
                foreach (IDirective directive in d.Directives)
                {
                    directives.Push(directive);
                }
            }
        }
    }
}
