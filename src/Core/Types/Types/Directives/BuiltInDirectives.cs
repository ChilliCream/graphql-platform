using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    internal static class BuiltInDirectives
    {
        public static IReadOnlyList<ITypeReference> All { get; } =
            new List<ITypeReference>
            {
                new ClrTypeReference(typeof(SkipDirectiveType), TypeContext.None),
                new ClrTypeReference(typeof(IncludeDirectiveType), TypeContext.None),
                new ClrTypeReference(typeof(CostDirectiveType), TypeContext.None)
            };
    }
}
