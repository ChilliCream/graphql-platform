using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using System;

namespace HotChocolate.Validation
{
    internal sealed class SubscriptionSingleRootFieldVisitor
        : QueryVisitorErrorBase
    {
        private int _fieldCount;

        public SubscriptionSingleRootFieldVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (OperationDefinitionNode operation in document.Definitions
                .OfType<OperationDefinitionNode>()
                .Where(t => t.Operation == OperationType.Subscription))
            {
                _fieldCount = 0;

                VisitOperationDefinition(operation,
                    path.Push(document));

                if (_fieldCount > 1)
                {
                    Errors.Add(new ValidationError(
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
                    .FirstOrDefault(t => t.Name.Value
                        .Equals(fragmentSpread.Name.Value,
                            StringComparison.Ordinal));

                if (fragment != null)
                {
                    VisitFragmentDefinition(fragment, path.Push(fragmentSpread));
                }
            }
        }
    }
}
