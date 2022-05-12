using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Language.Rewriters.Extensions;

public static class SourceDirectiveExtensions
{
    public static bool TryGetSource(this IHasDirectives hasDirectives,
        [MaybeNullWhen(false)] out SourceDirective sourceDirective)
    {
        sourceDirective = hasDirectives.Directives
            .FirstOrDefaultWhereNotNull(x =>
                SourceDirective.TryParse(x, out SourceDirective? directive) ? directive : default);

        return sourceDirective is not null;
    }

    public static TProjection? FirstOrDefaultWhereNotNull<TElement, TProjection>(
        this IEnumerable<TElement> element, Func<TElement, TProjection?> predicate)
    {
        using IEnumerator<TElement> enumerator = element.GetEnumerator();
        while (enumerator.MoveNext())
        {
            TProjection? result = predicate.Invoke(enumerator.Current);
            if (result is null) continue;

            return result;
        }

        return default;
    }
}
