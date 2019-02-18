using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class ScalarTypeMergeHandler
        : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public ScalarTypeMergeHandler(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.Any(t => IsScalarType(t.Definition)))
            {
                IReadOnlyList<ITypeInfo> current =
                    types.Where(t => !IsScalarType(t.Definition)).ToList();

                if (current.Count > 0)
                {
                    _next.Invoke(context, current);
                }
            }
            else
            {
                _next.Invoke(context, types);
            }
        }

        public static bool IsScalarType(ITypeDefinitionNode definition)
        {
            return definition is ScalarTypeDefinitionNode;
        }
    }
}
