using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Stitching;

internal static class HasDirectiveExtensions
{
    public static bool TryGetSourceDirective(
        this IHasDirectives hasDirectives,
        string schemaName,
        [NotNullWhen(true)] out SourceDirective? sourceDirective)
    {
        sourceDirective = hasDirectives.Directives[DirectiveNames.Source]
            .Select(t => t.AsValue<SourceDirective>())
            .FirstOrDefault(t => schemaName.Equals(t.Schema));
        return sourceDirective != null;
    }

    public static bool TryGetSourceName(
        this IHasDirectives hasDirectives,
        string schemaName,
        [NotNullWhen(true)] out string? sourceName)
    {
        if (TryGetSourceDirective(hasDirectives, schemaName, out var sd))
        {
            sourceName = sd.Name;
            return true;
        }

        sourceName = null;
        return false;
    }
}
