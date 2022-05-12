using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language.Rewriters;

public static class SourceDirectiveExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSource(this IHasDirectives hasDirectives,
        [MaybeNullWhen(false)] out SourceDirective sourceDirective)
    {
        for (var index = 0; index < hasDirectives.Directives.Count; index++)
        {
            DirectiveNode directive = hasDirectives.Directives[index];
            if (!SourceDirective.TryParse(directive, out sourceDirective))
            {
                continue;
            }

            return true;
        }

        sourceDirective = default;
        return false;
    }
}
