using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public static class Directives
    {
        private static readonly HashSet<string> _directiveNames =
            new HashSet<string>
            {
                WellKnownDirectives.Skip,
                WellKnownDirectives.Include,
                WellKnownDirectives.Deprecated,
                "cost"
            };

        internal static IReadOnlyList<ITypeReference> All { get; } =
            new List<ITypeReference>
            {
                new ClrTypeReference(typeof(SkipDirectiveType), TypeContext.None),
                new ClrTypeReference(typeof(IncludeDirectiveType), TypeContext.None),
                new ClrTypeReference(typeof(CostDirectiveType), TypeContext.None)
            };

        public static bool IsBuiltIn(NameString typeName)
        {
            return typeName.HasValue
                && _directiveNames.Contains(typeName.Value);
        }
    }
}
