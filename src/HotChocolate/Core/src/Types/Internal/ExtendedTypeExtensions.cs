using System;
using System.Collections.Generic;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Internal
{
    public static class ExtendedTypeExtensions
    {
        public static bool IsAssignableFrom(
            this Type type,
            IExtendedType extendedType) =>
            type.IsAssignableFrom(extendedType.Type);

        public static IExtendedType RewriteNullability(
            this IExtendedType type,
            params bool?[] nullable)
        {
            if (nullable.Length == 0)
            {
                return type;
            }

            return RewriteNullability(type, nullable, 0);
        }

        private static IExtendedType RewriteNullability(
            IExtendedType type,
            bool?[] nullable,
            int i)
        {
            var next = i + 1;
            var makeNullable = nullable[i].HasValue && nullable[i]!.Value != type.IsNullable;
            IReadOnlyList<IExtendedType> typeArguments = type.TypeArguments;

            if (nullable.Length > next)
            {
                var args = new IExtendedType[type.TypeArguments.Count];

                for (var j = 0; j < type.TypeArguments.Count; j++)
                {
                    next += j;
                    args[j] = nullable.Length > next
                        ? RewriteNullability(type.TypeArguments[j], nullable, next)
                        : type.TypeArguments[j];
                }

                typeArguments = args;
            }

            if (makeNullable != type.IsNullable ||
                !ReferenceEquals(typeArguments, type.TypeArguments))
            {
                type = new ExtendedType(
                    type.Type,
                    makeNullable,
                    type.Kind,
                    type.IsList,
                    type.IsNamedType,
                    typeArguments,
                    type.OriginalType);
            }

            return type;
        }
    }
}
