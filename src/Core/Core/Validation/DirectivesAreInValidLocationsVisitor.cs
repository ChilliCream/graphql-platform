using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreInValidLocationsVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, DirectiveType> _directives;

        public DirectivesAreInValidLocationsVisitor(ISchema schema)
            : base(schema)
        {
            _directives = schema.DirectiveTypes.ToDictionary(t => t.Name);
        }

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(directive.Name.Value, out DirectiveType d)
                && TryLookupLocation(path.Peek(),
                    out Types.DirectiveLocation location)
                && !d.Locations.Contains(location))
            {
                Errors.Add(new ValidationError(
                    "The specified directive is not valid the " +
                    "current location.", directive));
            }
        }

        private bool TryLookupLocation(
            ISyntaxNode syntaxNode,
            out Types.DirectiveLocation location)
        {
            if (syntaxNode is FieldNode)
            {
                location = Types.DirectiveLocation.Field;
                return true;
            }

            if (syntaxNode is FragmentDefinitionNode)
            {
                location = Types.DirectiveLocation.FragmentDefinition;
                return true;
            }

            if (syntaxNode is FragmentSpreadNode)
            {
                location = Types.DirectiveLocation.FragmentSpread;
                return true;
            }

            if (syntaxNode is InlineFragmentNode)
            {
                location = Types.DirectiveLocation.InlineFragment;
                return true;
            }

            if (syntaxNode is OperationDefinitionNode o)
            {
                switch (o.Operation)
                {
                    case OperationType.Query:
                        location = Types.DirectiveLocation.Query;
                        return true;

                    case OperationType.Mutation:
                        location = Types.DirectiveLocation.Mutation;
                        return true;

                    case OperationType.Subscription:
                        location = Types.DirectiveLocation.Subscription;
                        return true;

                    default:
                        location = default;
                        return false;
                }
            }

            location = default;
            return false;
        }
    }
}
