using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadTypeExistenceVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, List<FragmentSpreadNode>> _missingFragments =
            new Dictionary<string, List<FragmentSpreadNode>>();

        public FragmentSpreadTypeExistenceVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            if (!IsFragmentVisited(fragmentDefinition))
            {
                if (fragmentDefinition.TypeCondition?.Name?.Value == null
                    || !Schema.TryGetType(
                            fragmentDefinition.TypeCondition.Name.Value,
                            out INamedOutputType typeCondition))
                {
                    Errors.Add(new ValidationError(
                        $"The type of fragment `{fragmentDefinition.Name.Value}` " +
                        "does not exist in the current schema.",
                        fragmentDefinition));
                }
            }

            base.VisitFragmentDefinition(fragmentDefinition, path);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode inlineFragment,
            IType parentType,
            IType typeCondition,
            ImmutableStack<ISyntaxNode> path)
        {
            if (typeCondition == null)
            {
                Errors.Add(new ValidationError(
                    "The specified inline fragment " +
                    "does not exist in the current schema.",
                    inlineFragment));
            }

            base.VisitInlineFragment(
                inlineFragment,
                parentType,
                typeCondition,
                path);
        }
    }
}
