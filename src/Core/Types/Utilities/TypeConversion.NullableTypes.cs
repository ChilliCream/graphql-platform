using System;
using System.ComponentModel;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
    {
        private bool TryCreateNullableConverter(
            Type from, Type to, out ChangeType converter)
        {
            Type innerFrom = GetUnderlyingNullableType(from);
            Type innerTo = GetUnderlyingNullableType(to);

            if ((innerFrom != from || innerTo != to)
                && TryGetOrCreateConverter(innerFrom, innerTo, out converter))
            {
                Register(from, to, converter);
                return true;
            }

            converter = null;
            return false;
        }

        private Type GetUnderlyingNullableType(Type type)
        {
            if (type.IsValueType && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullableConverter = new NullableConverter(type);
                return nullableConverter.UnderlyingType;
            }
            return type;
        }
    }
}
