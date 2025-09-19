using HotChocolate.Types;

namespace HotChocolate.Internal;

internal sealed partial class ExtendedType
{
    private static class BaseTypes
    {
        private static readonly HashSet<Type> s_baseTypes =
        [
            typeof(ScalarType),
            typeof(InputObjectType),
            typeof(InputObjectTypeExtension),
            typeof(InputObjectType<>),
            typeof(EnumType),
            typeof(EnumTypeExtension),
            typeof(EnumType<>),
            typeof(ObjectType),
            typeof(ObjectTypeExtension),
            typeof(ObjectType<>),
            typeof(ObjectTypeExtension<>),
            typeof(InterfaceType),
            typeof(InterfaceTypeExtension),
            typeof(InterfaceType<>),
            typeof(UnionType),
            typeof(UnionTypeExtension),
            typeof(UnionType<>),
            typeof(DirectiveType),
            typeof(DirectiveType<>)
        ];

        /// <summary>
        /// Defines if the specified type is a named type that can be instantiated.
        /// </summary>
        public static bool IsNamedType(Type type)
        {
            if (type.IsAbstract || IsNonGenericBaseType(type))
            {
                return false;
            }

            if (IsGenericBaseType(type))
            {
                return true;
            }

            foreach (var baseType in s_baseTypes)
            {
                if (baseType.IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsGenericBaseType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type.IsGenericType && s_baseTypes.Contains(type.GetGenericTypeDefinition()))
            {
                return true;
            }

            return false;
        }

        public static bool IsNonGenericBaseType(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return s_baseTypes.Contains(type);
        }
    }
}
