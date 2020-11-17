using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal static class HasDirectiveExtensions
    {
        public static bool TryGetSourceDirective(
            this IHasDirectives hasDirectives,
            NameString schemaName,
            [NotNullWhen(true)] out SourceDirective? sourceDirective)
        {
            sourceDirective = hasDirectives.Directives[DirectiveNames.Source]
                .Select(t => t.ToObject<SourceDirective>())
                .FirstOrDefault(t => schemaName.Equals(t.Schema));
            return sourceDirective != null;
        }

        public static bool TryGetSourceName(
            this IHasDirectives hasDirectives,
            NameString schemaName,
            [NotNullWhen(true)] out NameString? sourceName)
        {
            if (TryGetSourceDirective(hasDirectives, schemaName, out SourceDirective? sd))
            {
                sourceName = sd.Name;
                return true;
            }

            sourceName = null;
            return false;
        }
    }
}
