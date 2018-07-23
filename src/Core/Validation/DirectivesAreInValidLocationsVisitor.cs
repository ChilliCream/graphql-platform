using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class DirectivesAreInValidLocationsVisitor
        : QueryVisitor
    {
        private readonly Dictionary<string, Directive> _directives;
        private readonly List<ValidationError> _errors =
            new List<ValidationError>();

        public DirectivesAreInValidLocationsVisitor(ISchema schema)
            : base(schema)
        {
            _directives = schema.Directives.ToDictionary(t => t.Name);
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        protected override void VisitDirective(
            DirectiveNode directive,
            ImmutableStack<ISyntaxNode> path)
        {
            if (_directives.TryGetValue(directive.Name.Value, out Directive d)
                && TryLookupLocation(path.Peek(),
                    out Types.DirectiveLocation location)
                && !d.Locations.Contains(location))
            {
                _errors.Add(new ValidationError(
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
