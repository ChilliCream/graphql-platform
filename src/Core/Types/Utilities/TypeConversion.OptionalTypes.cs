using System;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
    {
        private bool TryCreateOptionalConverter(
            Type from, Type to, out ChangeType converter)
        {
            Type innerFrom = GetUnderlyingOptionalType(from);
            Type innerTo = GetUnderlyingOptionalType(to);

            if ((innerFrom != from || innerTo != to)
                && TryGetOrCreateConverter(innerFrom, innerTo, out converter))
            {
                ChangeType innerConverter = converter;
                converter = source => GenericOptionalConverter((IOptional)source, innerConverter);
                Register(from, to, converter);
                return true;
            }

            converter = null;
            return false;
        }

        private static object GenericOptionalConverter(IOptional source, ChangeType converter)
        {
            if (source.HasValue)
            {
                return converter(source.Value);
            }

            return null;
        }

        private static Type GetUnderlyingOptionalType(Type type)
        {
            if (type.IsValueType 
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Optional<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }
    }
}
