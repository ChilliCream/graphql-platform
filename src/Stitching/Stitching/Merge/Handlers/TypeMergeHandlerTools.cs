using System.Collections.Generic;

namespace HotChocolate.Stitching
{
    internal static class TypeMergeHandlerTools
    {
        public static void MoveType(
           this ITypeInfo type,
           ICollection<ITypeInfo> from,
           ICollection<ITypeInfo> to)
        {
            from.Remove(type);
            to.Add(type);
        }
    }
}
