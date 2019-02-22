using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    internal class InterfaceTypeMergeHandler
        : TypeMergeHanlderBase<InterfaceTypeInfo>
    {
        public InterfaceTypeMergeHandler(MergeTypeRuleDelegate next)
            : base(next)
        {
        }

        protected override void MergeTypes(
            ISchemaMergeContext context,
            IReadOnlyList<InterfaceTypeInfo> types,
            NameString newTypeName)
        {
            List<InterfaceTypeDefinitionNode> definitions = types
                .Select(t => t.Definition)
                .ToList();

            InterfaceTypeDefinitionNode definition =
                definitions[0].Rename(
                    newTypeName,
                    types.Select(t => t.Schema.Name));

            context.AddType(definition);
        }

        protected override bool CanBeMerged(
            InterfaceTypeInfo left, InterfaceTypeInfo right) =>
            left.CanBeMergedWith(right);
    }
}
