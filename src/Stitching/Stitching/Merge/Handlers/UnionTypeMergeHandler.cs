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
            var notMerged = new List<ITypeInfo>();

            for (int i = 0; i < types.Count; i++)
            {
                if (types[i].Definition is UnionTypeDefinitionNode def)
                {
                    if (context.ContainsType(def.Name.Value))
                    {
                        context.AddType(def.WithName(
                            new NameNode(types[i].CreateUniqueName())));
                    }
                    else
                    {
                        context.AddType(def);
                    }
                }
                else
                {
                    notMerged.Add(types[i]);
                }
            }

            _next.Invoke(context, notMerged);
        }
    }
}
