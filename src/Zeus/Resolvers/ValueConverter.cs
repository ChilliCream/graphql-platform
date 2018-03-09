using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Zeus.Abstractions;

namespace Zeus.Resolvers
{
    public static class ValueConverter
    {
        private static readonly ScalarValueConverter _scalarConverter = new ScalarValueConverter();
        private static readonly EnumValueConverter _enumValueConverter = new EnumValueConverter();
        private static readonly ListValueConverter _listValueConverter = new ListValueConverter();
        private static readonly InputObjectValueConverter _inputObjectConverter = new InputObjectValueConverter();


        public static T Convert<T>(IValue value)
        {
            if (value is NullValue)
            {
                return (T)(object)null;
            }

            if (value is IScalarValue)
            {
                return _scalarConverter.Convert<T>(value);
            }

            if (value is EnumValue)
            {
                return _enumValueConverter.Convert<T>(value);
            }

            if (value is ListValueConverter)
            {
                return _listValueConverter.Convert<T>(value);
            }

            if (value is InputObjectValue)
            {
                return _inputObjectConverter.Convert<T>(value);
            }

            throw new NotSupportedException();
        }

    }

    internal class ListValueConverter
        : IValueConverter
    {
        public T Convert<T>(IValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is ListValue lv)
            {
                List<object> list = new List<object>();
                foreach (IValue item in lv.Items)
                {
                    list.Add(ValueConverter.Convert<object>(item));
                }

                // todo : map dictionary to custom object graphs
                throw new NotSupportedException();
            }

            throw new ArgumentException(
                "The specified type is not an list type.",
                nameof(value));
        }
    }


    internal class InputObjectValueConverter
        : IValueConverter
    {
        public T Convert<T>(IValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is InputObjectValue iov)
            {
                Type targetType = typeof(T);

                if (typeof(Dictionary<string, object>).IsAssignableFrom(targetType))
                {
                    return (T)(object)CreateInputObject(iov.Fields);
                }

                if (typeof(string) == typeof(T))
                {
                    Dictionary<string, object> obj = CreateInputObject(iov.Fields);
                    return (T)(object)JsonConvert.SerializeObject(obj);
                }

                // todo : map dictionary to custom object graphs
                throw new NotSupportedException();
            }

            throw new ArgumentException(
                "The specified type is not an input object type.",
                nameof(value));
        }

        private Dictionary<string, object> CreateInputObject(IEnumerable<KeyValuePair<string, IValue>> fields)
        {
            Dictionary<string, object> inputObject = new Dictionary<string, object>();
            foreach (KeyValuePair<string, IValue> field in fields)
            {
                inputObject[field.Key] = ValueConverter.Convert<object>(field.Value);
            }
            return inputObject;
        }
    }
}