using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class FragmentSpreadTargetDefinedVisitor
        : QueryVisitorErrorBase
    {
        private readonly Dictionary<string, List<FragmentSpreadNode>> _missingFragments =
            new Dictionary<string, List<FragmentSpreadNode>>();

        public FragmentSpreadTargetDefinedVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitDocument(
            DocumentNode document,
            ImmutableStack<ISyntaxNode> path)
        {
            base.VisitDocument(document, path);

            foreach (KeyValuePair<string, List<FragmentSpreadNode>> item in
                _missingFragments)
            {
                Errors.Add(new ValidationError(
                    $"The specified fragment `{item.Key}` does not exist.",
                    item.Value));
            }
        }

        protected override void VisitFragmentSpread(
            FragmentSpreadNode fragmentSpread,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            if (!ContainsFragment(fragmentSpread.Name.Value))
            {
                string fragmentName = fragmentSpread.Name.Value;
                if (!_missingFragments.TryGetValue(fragmentName,
                    out List<FragmentSpreadNode> f))
                {
                    f = new List<FragmentSpreadNode>();
                    _missingFragments[fragmentName] = f;
                }
                f.Add(fragmentSpread);
            }

            base.VisitFragmentSpread(fragmentSpread, type, path);
        }
    }
}
