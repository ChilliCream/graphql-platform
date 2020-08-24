using System;
using System.Collections.Generic;

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
            return nullable.Length == 0
                ? type
                : TypeCache.GetOrCreateType(() =>  RewriteNullability(type, nullable, 0));
        }

        private static ExtendedType RewriteNullability(
            IExtendedType type,
            bool?[] nullable,
            int i)
        {
            var next = i + 1;
            var changeNullability = nullable[i].HasValue && nullable[i]!.Value != type.IsNullable;
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

            if (changeNullability ||
                !ReferenceEquals(typeArguments, type.TypeArguments))
            {
                IExtendedType? elementType = type.IsArrayOrList ? type.GetElementType() : null;

                if (elementType is not null && typeArguments != type.TypeArguments)
                {
                    for (int e = 0; e < type.TypeArguments.Count; e++)
                    {
                        if (elementType == type.TypeArguments[e])
                        {
                            elementType = typeArguments[e];
                        }
                    }
                }

                type = new ExtendedType(
                    type.Type,
                    nullable[i]!.Value,
                    type.Kind,
                    type.IsList,
                    type.IsNamedType,
                    typeArguments,
                    type.OriginalType,
                    elementType);
            }

            return (ExtendedType)type;
        }

        public static IExtendedType ToExtendedType(this Type type) =>
            ExtendedType.FromType(type);
    }
}
