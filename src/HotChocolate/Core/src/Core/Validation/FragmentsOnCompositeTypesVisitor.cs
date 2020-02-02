using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FragmentsOnCompositeTypesVisitor
        : QueryVisitorErrorBase
    {
        private readonly List<ISyntaxNode> _fragmentErrors =
            new List<ISyntaxNode>();

        public FragmentsOnCompositeTypesVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            _fragmentErrors.Clear();

            base.VisitDocument(document, path);

            if (_fragmentErrors.Count > 0)
            {
                Errors.Add(new ValidationError(
                    "Fragments can only be declared on unions, interfaces, " +
                    "and objects.", _fragmentErrors));
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode fragmentDefinition,
            ImmutableStack<ISyntaxNode> path)
        {
            ValidateTypeCondition(
                fragmentDefinition,
                fragmentDefinition.TypeCondition.Name.Value);

            base.VisitFragmentDefinition(fragmentDefinition, path);
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode inlineFragment,
            IType parentType,
            IType typeCondition,
            ImmutableStack<ISyntaxNode> path)
        {
            ValidateTypeCondition(
                inlineFragment,
                typeCondition);

            base.VisitInlineFragment(
                inlineFragment,
                parentType,
                typeCondition,
                path);
        }

        private void ValidateTypeCondition(
            ISyntaxNode syntaxNode,
            string typeCondition)
        {
            if (typeCondition != null
                && Schema.TryGetType(typeCondition, out INamedType type))
            {
                ValidateTypeCondition(syntaxNode, type);
            }
        }

        private void ValidateTypeCondition(
            ISyntaxNode syntaxNode,
            IType typeCondition)
        {
            if (typeCondition != null
                && !typeCondition.IsCompositeType())
            {
                _fragmentErrors.Add(syntaxNode);
            }
        }
    }
}
