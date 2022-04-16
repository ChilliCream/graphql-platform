using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Stitching.Types.Extensions;

public static class ListElementReplacementExtensions
{
    public static IReadOnlyList<TElement> AddOrReplace<TList, TElement>(this TList list, TElement replacement, Func<TElement, bool> filter)
        where TList : class, IEnumerable<TElement>
    {
        var updatedList = list.Select(element => filter(element) ? replacement : element)
            .ToList();

        var exists = updatedList.Any(x => ReferenceEquals(replacement, x));
        if (exists)
        {
            return updatedList;
        }

        updatedList.Add(replacement);

        return updatedList;
    }
}
