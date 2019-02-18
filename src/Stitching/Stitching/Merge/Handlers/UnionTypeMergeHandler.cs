using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
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
                    string name = def.Name.Value;

                    if (context.ContainsType(name))
                    {
                        name = types[i].CreateUniqueName();
                    }

                    context.AddType(def.AddSource(
                        name,
                        types.Select(t => t.Schema.Name)));
                }
                else
                {
                    notMerged.Add(types[i]);
                }
            }

            if (notMerged.Count > 0)
            {
                _next.Invoke(context, notMerged);
            }
        }
    }
}
