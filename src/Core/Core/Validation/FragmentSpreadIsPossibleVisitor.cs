using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadIsPossibleVisitor
        : QueryVisitorErrorBase
    {
        public FragmentSpreadIsPossibleVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (type is INamedType parentType
                && TryGetFragment(fragmentSpread.Name.Value,
                out FragmentDefinitionNode fragment)
                && Schema.TryGetType(fragment.TypeCondition.Name.Value,
                out INamedOutputType typeCondition)
                && parentType.IsCompositeType()
                && typeCondition.IsCompositeType()
                && !GetPossibleType(parentType)
                    .Intersect(GetPossibleType(typeCondition))
                    .Any())
            {
                Errors.Add(new ValidationError(
                    "The parent type does not match the type condition on " +
                    $"the fragment `{fragment.Name}`.", fragmentSpread));
            }

            base.VisitFragmentSpread(fragmentSpread, type, path);
        }

        private IEnumerable<IType> GetPossibleType(INamedType type)
        {
            if (type is ObjectType ot)
            {
                return new[] { ot };
            }

            return Schema.GetPossibleTypes(type);
        }
    }
}
