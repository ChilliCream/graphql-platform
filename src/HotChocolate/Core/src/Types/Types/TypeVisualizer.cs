using System;

namespace HotChocolate.Types
{
    public static class TypeVisualizer
    {
        private const int _maxTypeDepth = 6;

        [Obsolete("Use Print")]
        public static string Visualize(this IType type) => Print(type);

        public static string Print(this IType type)
        {
            return Print(type, 0);
        }

        private static string Print(IType type, int count)
        {
            if (count > _maxTypeDepth)
            {
                throw new InvalidOperationException(
                    "A type can only consist of four components.");
            }

            if (type is NonNullType nnt)
            {
                return $"{Print(nnt.Type, ++count)}!";
            }

            if (type is ListType lt)
            {
                return $"[{Print(lt.ElementType, ++count)}]";
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
