using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Internal
{
    internal static class BaseTypes
    {
        private static HashSet<Type> _baseTypes = new HashSet<Type>
        {
            typeof(ScalarType),
            typeof(InputObjectType),
            typeof(InputObjectType<>),
            typeof(ObjectType),
            typeof(ObjectType<>),
            typeof(EnumType),
            typeof(EnumType<>),
            typeof(InterfaceType),
            typeof(UnionType)
        };

        public static bool IsBaseType(Type type)
        {
            if (_baseTypes.Contains(type)
                || (type.IsGenericType
                    && _baseTypes.Contains(
                        type.GetGenericTypeDefinition())))
            {
                return true;
            }
            return false;
        }

        public static bool IsNonGenericBaseType(Type type)
        {
            if (_baseTypes.Contains(type))
            {
                return true;
            }
            return false;
        }
    }
}
