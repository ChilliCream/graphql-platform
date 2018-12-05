using System;
using System.Collections;
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
            if (from == to)
            {
                output = input;
                return true;
            }

            if (TryGetConverter(from, to, out ChangeType converter))
            {
                output = converter(input);
                return true;
            }

            Type fromElement = DotNetTypeInfoFactory.GetInnerListType(from);
            Type toElement = DotNetTypeInfoFactory.GetInnerListType(to);

            if (fromElement != null && toElement != null && input is ICollection collection)
            {
                if (to.IsArray)
                {
                    return TryChangeListTypeToArrayType(
                        fromElement, toElement, collection,
                        out output);
                }
                else if (to.IsInterface)
                {
                    Type type = typeof(List<>).MakeGenericType(toElement);
                    IList array = (IList)Activator.CreateInstance(type);
                    output = array;
                    return TryChangeListType(fromElement,
                        toElement, collection, (item, index) => array.Add(item));
                }
                else if (to.IsClass)
                {
                    IList array = (IList)Activator.CreateInstance(to);
                    output = array;
                    return TryChangeListType(fromElement,
                        toElement, collection, (item, index) => array.Add(item));
                }

            }

            output = null;
            return false;
        }

        private bool TryChangeListType(Type from, Type to, IEnumerable source,
            Action<object, int> addToDestination)
        {
            if (TryGetConverter(from, to, out ChangeType converter))
            {
                int i = 0;
                foreach (object item in source)
                {
                    addToDestination(item, i++);
                }
                return true;
            }

            return false;
        }

        private bool TryChangeListTypeToArrayType(Type from, Type to,
            ICollection source, out object destination)
        {
            Array array = Array.CreateInstance(to, source.Count);

            if (TryChangeListType(from, to, source,
                (item, index) => array.SetValue(item, index)))
            {
                destination = array;
                return true;
            };

            destination = null;
            return false;
        }

        private bool TryGetConverter(
            Type from, Type to,
            out ChangeType converter)
        {
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

    public partial class TypeConversion
    {

    }
}
