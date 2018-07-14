using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class SubscriptionSingleRootFieldVisitor
        : QueryVisitor
    {
        private int _fieldCount;
        private List<ValidationError> _errors = new List<ValidationError>();

        public SubscriptionSingleRootFieldVisitor(ISchema schema)
            : base(schema)
        {
        }

        public IReadOnlyCollection<ValidationError> Errors => _errors;

        public override void VisitDocument(DocumentNode document)
        {
            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>()
                .Where(t => t.Operation == OperationType.Subscription))
            {
                _fieldCount = 0;

                VisitOperationDefinition(operation,
                    ImmutableStack<ISyntaxNode>.Empty.Push(document));

                if (_fieldCount > 1)
                {
                    _errors.Add(new ValidationError(
                        $"Subscription operation `{operation.Name.Value}` " +
                        "must have exactly one root field.", operation));
                }
            }
        }

        protected override void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            _fieldCount++;
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (path.Last() is DocumentNode d)
            {
                FragmentDefinitionNode fragment = d.Definitions
                    .OfType<FragmentDefinitionNode>()
                    .FirstOrDefault(t =>
                        t.Name.Value.EqualsOrdinal(fragmentSpread.Name.Value));
                if (fragment != null)
                {
                    VisitFragmentDefinition(fragment, path.Push(fragmentSpread));
                }
            }
        }
    }
}
