using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching.Types.Extensions;

public static class ListElementReplacementExtensions
{
    public static void AddIfNotExist<TList, TElement>(this TList list, TElement element)
        where TList : ICollection<TElement>
    {
        if (list.Contains(element))
        {
            return;
        }

        list.Add(element);
    }

    public static IReadOnlyList<TElement> AddOrReplace<TList, TElement>(this TList list, TElement replacement, Func<TElement, bool> filter)
        where TList : class, IEnumerable<TElement>
    {
        var updatedList = new List<TElement>(list);

        var replaced = false;
        for (var i = 0; i < updatedList.Count; i++)
        {
            TElement element = updatedList[i];
            if (!filter(element))
            {
                continue;
            }

            updatedList[i] = replacement;
            replaced = true;
        }

        if (!replaced)
        {
            updatedList.Add(replacement);
        }

        return updatedList;
    }
}
