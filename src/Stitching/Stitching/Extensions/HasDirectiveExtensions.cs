using System.Linq;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    internal static class HasDirectiveExtensions
    {
        public static bool TryGetSourceDirective(
            this IHasDirectives hasDirectives,
            NameString schemaName,
            out SourceDirective sourceDirective)
        {
            sourceDirective = hasDirectives.Directives[DirectiveNames.Source]
                .Select(t => t.ToObject<SourceDirective>())
                .FirstOrDefault(t => schemaName.Equals(t.Schema));
            return sourceDirective != null;
        }
    }
}
