using System.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Utilities
{
    internal class ObjectToDictionaryConverter
    {
        private readonly ITypeConversion _converter;
        private readonly ConcurrentDictionary<Type, List<PropertyInfo>> _properties =
            new ConcurrentDictionary<Type, List<PropertyInfo>>();

        public ObjectToDictionaryConverter(ITypeConversion converter)
        {
            _converter = converter
                ?? throw new ArgumentNullException(nameof(converter));
        }

        public object Convert(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            object value = null;
            Action<object> setValue = v => value = v;
            VisitValue(obj, setValue, new HashSet<object>());
            return value;
        }

        private void VisitValue(
            object obj,
            Action<object> setValue,
            ISet<object> processed)
        {
            if (obj is null)
            {
                setValue(null);
                return;
            }

            switch (obj)
            {
                case string _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case float _:
                case double _:
                case decimal _:
                case bool _:
                    setValue(obj);
                    return;
            }

            Type type = obj.GetType();

            if (type.IsValueType && _converter.TryConvert(
                type, typeof(string), obj, out object converted)
                && converted is string s)
            {
                setValue(s);
                return;
            }
            else if (!typeof(IReadOnlyDictionary<string, object>).IsAssignableFrom(type)
                && obj is ICollection list)
            {
                VisitList(list, setValue, processed);
            }
            else
            {
                VisitObject(obj, setValue, processed);
            }
        }

        private void VisitObject(
            object obj,
            Action<object> setValue,
            ISet<object> processed)
        {
            if (processed.Add(obj))
            {
                var dict = new Dictionary<string, object>();
                setValue(dict);

                if (obj is IReadOnlyDictionary<string, object> d)
                {
                    foreach (KeyValuePair<string, object> item in d)
                    {
                        Action<object> setField = v => dict[item.Key] = v;
                        VisitValue(item.Value, setField, processed);
                    }
                }
                else
                {
                    foreach (PropertyInfo property in GetProperties(obj))
                    {
                        string name = property.GetGraphQLName();
                        object value = property.GetValue(obj);
                        Action<object> setField = v => dict[name] = v;
                        VisitValue(value, setField, processed);
                    }
                }
            }
        }

        private void VisitList(
            ICollection list,
            Action<object> setValue,
            ISet<object> processed)
        {
            var valueList = new List<object>();
            setValue(valueList);

            Action<object> addItem = item => valueList.Add(item);

            foreach (object element in list)
            {
                VisitValue(element, addItem, processed);
            }
        }

        private IReadOnlyList<PropertyInfo> GetProperties(object value)
        {
            Type type = value.GetType();
            if (!_properties.TryGetValue(type, out List<PropertyInfo> properties))
            {
                properties = new List<PropertyInfo>(
                    ReflectionUtils.GetProperties(type).Values);
                _properties.TryAdd(type, properties);
            }
            return properties;
        }
    }
}
