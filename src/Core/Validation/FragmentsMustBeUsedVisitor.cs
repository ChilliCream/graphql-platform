using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal sealed class FragmentsMustBeUsedVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, FragmentDefinitionNode> _fragments =
            new Dictionary<string, FragmentDefinitionNode>();

        public FragmentsMustBeUsedVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            _fragments.Clear();

            foreach (FragmentDefinitionNode fragment in
                document.Definitions.OfType<FragmentDefinitionNode>())
            {
                _fragments[fragment.Name.Value] = fragment;
            }

            VisitOperationDefinitions(
                document.Definitions.OfType<OperationDefinitionNode>(),
                path);

            foreach (FragmentDefinitionNode fragment in _fragments.Values)
            {
                Errors.Add(new ValidationError(
                    $"The specified fragment `{fragment.Name.Value}` " +
                    "is not used within the current document.",
                    fragment));
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            _fragments.Remove(fragmentDefinition.Name.Value);

            base.VisitFragmentDefinition(fragmentDefinition, path);
        }
    }
}
