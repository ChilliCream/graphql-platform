using System.Collections.Immutable;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadTypeExistenceVisitor
        : QueryVisitorErrorBase
    {
        public FragmentSpreadTypeExistenceVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            if (!IsFragmentVisited(fragmentDefinition)
                && (fragmentDefinition.TypeCondition?.Name?.Value == null
                    || !Schema.TryGetType(
                            fragmentDefinition.TypeCondition.Name.Value,
                            out INamedOutputType typeCondition)))
            {
                Errors.Add(ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        ValidationResources.UnknownType,
                        fragmentDefinition.TypeCondition.Name.Value))
                    .SetCode(ErrorCodes.Validation.UnknownType)
                    .AddLocation(fragmentDefinition.TypeCondition)
                    .Build());
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
                Errors.Add(ErrorBuilder.New()
                    .SetMessage(string.Format(
                        CultureInfo.InvariantCulture,
                        ValidationResources.UnknownType,
                        inlineFragment.TypeCondition.Name.Value))
                    .SetCode(ErrorCodes.Validation.UnknownType)
                    .AddLocation(inlineFragment.TypeCondition)
                    .Build());
            }

            base.VisitInlineFragment(
                inlineFragment,
                parentType,
                typeCondition,
                path);
        }
    }

    
}
