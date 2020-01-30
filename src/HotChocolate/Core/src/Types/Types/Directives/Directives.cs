using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types
{
    public static class Directives
    {
        private static readonly HashSet<NameString> _directiveNames =
            new HashSet<NameString>
            {
                WellKnownDirectives.Skip,
                WellKnownDirectives.Include,
                WellKnownDirectives.Deprecated
            };

        internal static IReadOnlyList<ITypeReference> All { get; } =
            new List<ITypeReference>
            {
                new ClrTypeReference(typeof(SkipDirectiveType), TypeContext.None),
                new ClrTypeReference(typeof(IncludeDirectiveType), TypeContext.None),
            };

        public static bool IsBuiltIn(NameString typeName)
        {
            return _directiveNames.Contains(typeName.Value);
        }
    }
}
