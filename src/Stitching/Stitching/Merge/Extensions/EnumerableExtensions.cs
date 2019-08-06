using System;
using System.Collections.Generic;

namespace HotChocolate.Stitching.Merge
{
    internal static class EnumerableExtensions
    {
        public static IReadOnlyList<ITypeInfo> NotOfType<T>(
            this IEnumerable<ITypeInfo> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var list = new List<ITypeInfo>();

            foreach (ITypeInfo type in types)
            {
                if (!(type is T))
                {
                    list.Add(type);
                }
            }

            return list;
        }
    }
}
