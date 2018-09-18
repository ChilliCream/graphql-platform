using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal class DirectiveCollector
    {
        private readonly ISchema _schema;

        public DirectiveCollector(ISchema schema)
        {
            _schema = schema
                ?? throw new ArgumentNullException(nameof(schema));
        }


        public IDirectiveCollection CollectDirectives(
            ObjectType objectType,
            ObjectField field,
            FieldNode fieldSelection)
        {
            // 1. selection
            // 2. field
            // 3. interface fields
            // 4. objects
            // 5. interfaces
            // 6. schema

            Stack<IDirective> directives = new Stack<IDirective>();

            CollectDirectives(directives, objectType.Interfaces.Values);
            CollectDirectives(directives, objectType);
            // CollectDirectives(directives, field);


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
                directives.Push(directive);
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

    internal interface IDirectiveCollection
    {
        IReadOnlyCollection<IDirective> Inherited { get; }
        IReadOnlyCollection<IDirective> FromSelection { get; }

    }
}
