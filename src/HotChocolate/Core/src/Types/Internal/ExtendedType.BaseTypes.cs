using System;
using System.Collections.Generic;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Internal
{
    internal sealed partial class ExtendedType
    {
        public static class BaseTypes
        {
            private static readonly HashSet<Type> _baseTypes = new HashSet<Type>
            {
                typeof(ScalarType),
                typeof(InputObjectType),
                typeof(InputObjectType<>),
                typeof(EnumType),
                typeof(EnumType<>),
                typeof(ObjectType),
                typeof(ObjectType<>),
                typeof(InterfaceType),
                typeof(InterfaceType<>),
                typeof(UnionType),
                typeof(UnionType<>),
                typeof(DirectiveType),
                typeof(DirectiveType<>)
            };

            public static bool IsGenericBaseType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                if (type.IsGenericType &&
                    _baseTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    return true;
                }
                return false;
            }

            public static bool IsNonGenericBaseType(Type type)
            {
                if (type is null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return _baseTypes.Contains(type);
            }
        }
    }
}
