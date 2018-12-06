using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace HotChocolate.Utilities
{
    public partial class TypeConversion
        : ITypeConversion
        , ITypeConverterRegistry
    {
        private Dictionary<Type, Dictionary<Type, ChangeType>> _converters =
            new Dictionary<Type, Dictionary<Type, ChangeType>>();

        public TypeConversion(IEnumerable<ITypeConverter> converters)
        {
            RegisterConverters(this);

            foreach (ITypeConverter converter in converters)
            {
                TypeConverterRegistryExtensions.Register(this, converter);
            }
        }

        public TypeConversion()
        {
            RegisterConverters(this);
        }

        public bool TryConvert(Type from, Type to,
            object source, out object converted)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (source is null || from == to)
            {
                converted = source;
                return true;
            }

            Type fromInternal = (from == typeof(object) && source != null)
                ? source.GetType()
                : from;

            return TryConvertInternal(fromInternal, to, source, out converted);
        }

        private bool TryConvertInternal(Type from, Type to,
            object input, out object output)
        {
            if (from == to)
            {
                output = input;
                return true;
            }

            if (TryGetConverter(from, to, out ChangeType converter)
                || TryCreateListTypeConverter(from, to, out converter)
                || TryCreateNullableConverter(from, to, out converter))
            {
                output = converter(input);
                return true;
            }

            output = null;
            return false;
        }

        private bool TryCreateNullableConverter(
            Type from, Type to, out ChangeType converter)
        {
            Type innerFrom = GetUnderlyingNullableType(from);
            Type innerTo = GetUnderlyingNullableType(to);

            if ((innerFrom != from || innerTo != to)
                && TryGetConverter(innerFrom, innerTo, out converter))
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

        private bool TryGetConverter(
            Type from, Type to,
            out ChangeType converter)
        {
            if (from == to)
            {
                converter = source => source;
                return true;
            }

            if (_converters.TryGetValue(from, out var toLookUp)
                && toLookUp.TryGetValue(to, out var changeType))
            {
                converter = changeType;
                return true;
            }

            converter = null;
            return false;
        }

        public void Register(Type from, Type to, ChangeType converter)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            lock (_converters)
            {
                if (!_converters.TryGetValue(from, out var toLookUp))
                {
                    toLookUp = new Dictionary<Type, ChangeType>();
                    _converters[from] = toLookUp;
                }
                toLookUp[to] = converter;
            }
        }

        public void Register<TFrom, TTo>(ChangeType<TFrom, TTo> converter)
        {
            Register(typeof(TFrom), typeof(TTo),
                from => converter((TFrom)from));
        }

        public static TypeConversion Default { get; } = new TypeConversion();
    }
}
