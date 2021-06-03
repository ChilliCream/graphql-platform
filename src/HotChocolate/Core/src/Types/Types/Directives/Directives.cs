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

        internal static IReadOnlyList<ITypeReference> CreateReferences(
            ITypeInspector typeInspector) =>
            new ITypeReference[]
            {
                typeInspector.GetTypeRef(typeof(SkipDirectiveType), TypeContext.None),
                typeInspector.GetTypeRef(typeof(IncludeDirectiveType), TypeContext.None),
                typeInspector.GetTypeRef(typeof(DeferDirectiveType), TypeContext.None),
                typeInspector.GetTypeRef(typeof(StreamDirectiveType), TypeContext.None),
            };

        public static bool IsBuiltIn(NameString typeName)
        {
            return _directiveNames.Contains(typeName.Value);
        }
    }
}
