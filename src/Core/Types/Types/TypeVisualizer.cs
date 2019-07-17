using System;

namespace HotChocolate.Types
{
    public static class TypeVisualizer
    {
        private const int _maxTypeDepth = 6;

        public static string Visualize(this IType type)
        {
            return Visualize(type, 0);
        }

        private static string Visualize(IType type, int count)
        {
            if (count > _maxTypeDepth)
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
