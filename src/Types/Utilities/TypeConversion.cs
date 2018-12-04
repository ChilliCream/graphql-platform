using System;
using System.Collections.Generic;

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
            object input, out object output)
        {
            if (_converters.TryGetValue(from, out var toLookUp)
                && toLookUp.TryGetValue(to, out var changeType))
            {
                output = changeType(input);
                return true;
            }

            output = null;
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
