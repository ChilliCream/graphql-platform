using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class ScalarTypeMergeHandler : TypeMergeHandlerBase<ScalarTypeInfo>
    {
        public ScalarTypeMergeHandler(MergeTypeRuleDelegate next)
            : base(next)
        {
        }

        protected override void MergeTypes(
            ISchemaMergeContext context,
            IReadOnlyList<ScalarTypeInfo> types,
            NameString newTypeName)
        {
            ScalarTypeInfo scalar =
                types.FirstOrDefault(t => t.Definition.Description is not null) ??
                    types.First();

            context.AddType(scalar.Definition);
        }

        protected override bool CanBeMerged(ScalarTypeInfo left, ScalarTypeInfo right)
            => left.Definition.Name.Value.Equals(right.Definition.Name.Value);
    }
}
