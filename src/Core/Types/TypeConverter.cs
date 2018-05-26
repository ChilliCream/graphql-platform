using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types
{
    internal static class TypeConverter
    {
        public static IOutputType CreateOutputType(
            SchemaContext context, Type nativeType)
        {
            IType type = CreateType(context, nativeType);
            if (type.IsObjectType())
            {
                return (IOutputType)type;
            }

            throw new ArgumentException(
                "The specified type is not an output type.",
                nameof(nativeType));
        }

        public static IInputType CreateInputType(
            SchemaContext context, Type nativeType)
        {
            IType type = CreateType(context, nativeType);
            if (type.IsInputType())
            {
                return (IInputType)type;
            }

            throw new ArgumentException(
                "The specified type is not an input type.",
                nameof(nativeType));
        }

        private static IType CreateType(SchemaContext context, Type nativeType)
        {
            if (typeof(IType).IsAssignableFrom(nativeType))
            {
                return ConvertNativeTypeToType(context, nativeType);
            }
            else
            {
                IOutputType type = context.GetTypes<ScalarType>()
                    .FirstOrDefault(t => t.NativeType == nativeType);
                if (type == null)
                {
                    type = context.GetTypes<EnumType>()
                        .FirstOrDefault(t => t.NativeType == nativeType);
                }
                return type;
            }
        }

        private static IType ConvertNativeTypeToType(
            SchemaContext context, Type nativeType)
        {
            List<Type> types = DecomposeType(nativeType);

            IType type;
            if (!TryCreate4ComponentType(context, types, out type)
                && !TryCreate3ComponentType(context, types, out type)
                && !TryCreate2ComponentType(context, types, out type)
                && !TryCreate1ComponentType(context, types, out type))
            {
                throw new NotSupportedException(
                    "The specified type is not supported in this context.");
            }

            return type;
        }

        private static List<Type> DecomposeType(Type type)
        {
            List<Type> types = new List<Type>();
            Type current = type;

            do
            {
                types.Add(current);
                current = GetInnerType(type);
            } while (current != null && types.Count < 4);

            return types;
        }

        private static bool TryCreate4ComponentType(
            SchemaContext context,
            List<Type> types,
            out IType type)
        {
            if (types.Count == 4
                && IsNonNullType(types[0])
                && IsListType(types[1])
                && IsNonNullType(types[2])
                && IsNamedType(types[3]))
            {
                type = new NonNullType(new ListType(new NonNullType(
                    context.GetOrCreateType<INamedType>(types[3]))));
                return true;
            }

            type = default;
            return false;
        }

        private static bool TryCreate3ComponentType(
            SchemaContext context,
            List<Type> types,
            out IType type)
        {
            if (types.Count == 3)
            {
                if (IsListType(types[0])
                    && IsNonNullType(types[1])
                    && IsNamedType(types[2]))
                {
                    type = new ListType(new NonNullType(
                        context.GetOrCreateType<INamedType>(types[2])));
                    return true;
                }

                if (IsNonNullType(types[0])
                    && IsListType(types[1])
                    && IsNamedType(types[2]))
                {
                    type = new NonNullType(new ListType(
                        context.GetOrCreateType<INamedType>(types[2])));
                    return true;
                }
            }

            type = default;
            return false;
        }

        private static bool TryCreate2ComponentType(
            SchemaContext context,
            List<Type> types,
            out IType type)
        {
            if (types.Count == 2)
            {
                if (IsNonNullType(types[0])
                    && IsNamedType(types[1]))
                {
                    type = new ListType(new NonNullType(
                        context.GetOrCreateType<INamedType>(types[1])));
                    return true;
                }

                if (IsListType(types[0])
                    && IsNamedType(types[1]))
                {
                    type = new NonNullType(new ListType(
                        context.GetOrCreateType<INamedType>(types[1])));
                    return true;
                }
            }

            type = default;
            return false;
        }

        private static bool TryCreate1ComponentType(
            SchemaContext context,
            List<Type> types,
            out IType type)
        {
            if (types.Count == 1
               && IsNamedType(types[0]))
            {
                type = context.GetOrCreateType<INamedType>(types[0]);
                return true;
            }

            type = default;
            return false;
        }

        private static Type GetInnerType(Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetGenericArguments().First();
            }
            return null;
        }

        private static bool IsListType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(ListType<>);
        }

        private static bool IsNonNullType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(NonNullType<>);
        }

        private static bool IsNamedType(Type type)
        {
            return type is INamedType;
        }
    }
}
