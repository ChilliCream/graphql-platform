using System;

namespace HotChocolate.Types
{
    internal static class TypeVisualizer
    {
        public static string Visualize(this IType type)
        {
            return Visualize(type, 0);
        }

        public static string Visualize(IType type, int count)
        {
            if (count > 3)
            {
                throw new InvalidOperationException(
                    "A type can only consist of four components.");
            }

            if (type is NonNullType nnt)
            {
                return $"{Visualize(nnt.Type, ++count)}!";
            }

            if (type is ListType lt)
            {
                return $"[{Visualize(lt.ElementType, ++count)}]";
            }

            if (type is INamedType n)
            {
                return n.Name;
            }

            throw new NotSupportedException(
                "The specified type is not supported.");
        }
    }
}
