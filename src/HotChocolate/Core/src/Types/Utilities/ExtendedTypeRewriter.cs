using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Internal;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Utilities
{
    internal static class ExtendedTypeRewriter
    {
        public static Type Rewrite(IExtendedType type, params Nullable[] nullable)
        {
            var components = new Stack<IExtendedType>();
            IExtendedType? current = type;
            var i = 0;

            do
            {
                if (!IsNonEssentialComponent(current))
                {
                    var makeNullable = current.IsNullable;

                    if (nullable.Length > i)
                    {
                        Nullable value = nullable[i++];
                        if (value != Nullable.Undefined)
                        {
                            makeNullable = value == Nullable.Yes;
                        }
                    }

                    if (current.IsNullable == makeNullable)
                    {
                        components.Push(current);
                    }
                    else
                    {
                        components.Push(new Internal.ExtendedType(
                            current.Type,
                            makeNullable,
                            current.Kind,
                            current.IsList,
                            current.IsNamedType,
                            current.TypeArguments,
                            current.OriginalType));
                    }
                }
                current = GetInnerType(current);
            } while (current != null && components.Count < 7);

            Type? rewritten = null;

            while (components.Count > 0)
            {
                if (rewritten is null)
                {
                    current = components.Pop();
                    rewritten = Rewrite(current.Type, current.IsNullable);
                }
                else
                {
                    current = components.Pop();
                    rewritten = current.IsArray && !typeof(IType).IsAssignableFrom(rewritten)
                        ? rewritten.MakeArrayType()
                        : MakeListType(rewritten);
                    rewritten = Rewrite(rewritten, current.IsNullable);
                }
            }

            return rewritten!;
        }

        private static Type Rewrite(Type type, bool isNullable)
        {
            if (type.IsValueType)
            {
                if (isNullable)
                {
                    return typeof(Nullable<>).MakeGenericType(type);
                }

                return type;
            }

            if (isNullable)
            {
                return type;
            }

            return MakeNonNullType(type);
        }

        private static Type MakeListType(Type elementType)
        {
            return typeof(List<>).MakeGenericType(elementType);
        }

        private static Type MakeNonNullType(Type nullableType)
        {
            Type wrapper = typeof(NativeType<>).MakeGenericType(nullableType);
            return typeof(NonNullType<>).MakeGenericType(wrapper);
        }


        private static bool IsNonEssentialComponent(IExtendedType type)
        {
            return IsTaskType(type);
        }

        public static IExtendedType? GetInnerType(IExtendedType type)
        {
            if (type.IsArrayOrList)
            {
                return type.GetElementType();
            }

            if (IsTaskType(type))
            {
                return type.TypeArguments[0];
            }

            return null;
        }

        private static bool IsTaskType(IExtendedType type)
        {
            return type.IsGeneric
                && (typeof(Task<>) == type.Definition
                    || typeof(ValueTask<>) == type.Definition);
        }
    }
}
