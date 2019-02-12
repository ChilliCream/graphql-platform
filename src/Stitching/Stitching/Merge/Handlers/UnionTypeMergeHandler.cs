using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class UnionTypeMergeHandler
        : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public UnionTypeMergeHandler(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.All(t => t.Definition is UnionTypeDefinitionNode))
            {
                var first = (UnionTypeDefinitionNode)types[0].Definition;

                for (int i = 1; i < types.Count; i++)
                {
                    var other = (UnionTypeDefinitionNode)types[i].Definition;
                    context.AddType(other.Rename(
                        types[i].CreateUniqueName(),
                        types[i].Schema.Name));
                }

                context.AddType(first);
            }
            else
            {
                _next.Invoke(context, types);
            }
        }
    }
}
