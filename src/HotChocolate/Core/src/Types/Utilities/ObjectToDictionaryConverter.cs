using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using HotChocolate.Properties;

namespace HotChocolate.Utilities;

internal class ObjectToDictionaryConverter
{
    private readonly ITypeConverter _converter;
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

    public ObjectToDictionaryConverter(ITypeConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
    }

    public object Convert(object obj)
    {
        if(obj is null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        object value = null;
        void SetValue(object v) => value = v;
        VisitValue(obj, SetValue, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return value;
    }

    private void VisitValue(
        object obj,
        Action<object> setValue,
        HashSet<object> processed)
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
            case sbyte _:
                setValue(obj);
                return;
        }

        var type = obj.GetType();

        if (type.IsValueType &&
            _converter.TryConvert(type, typeof(string), obj, out var converted) &&
            converted is string s)
        {
            setValue(s);
        }
        else if (!typeof(IReadOnlyDictionary<string, object>).IsAssignableFrom(type) &&
            obj is ICollection list)
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
        HashSet<object> processed)
    {
        if (processed.Add(obj))
        {
            var current = new Dictionary<string, object>();
            setValue(current);

            if (obj is Dictionary<string, object> dict1)
            {
                foreach (var item in dict1)
                {
                    void SetField(object v) => current[item.Key] = v;
                    VisitValue(item.Value, SetField, processed);
                }
            }
            else if (obj is IDictionary<string, object> dict2)
            {
                foreach (var item in dict2)
                {
                    void SetField(object v) => current[item.Key] = v;
                    VisitValue(item.Value, SetField, processed);
                }
            }
            else if (obj is IReadOnlyDictionary<string, object> dict3)
            {
                foreach (var item in dict3)
                {
                    void SetField(object v) => current[item.Key] = v;
                    VisitValue(item.Value, SetField, processed);
                }
            }
            else if (obj is IDictionary dict4)
            {
                foreach (var item in dict4)
                {
                    if (item is DictionaryEntry entry)
                    {
                        void SetField(object v) => current[entry.Key.ToString()!] = v;
                        VisitValue(entry.Value, SetField, processed);
                    }
                    else if (item is KeyValuePair<string, object> pair)
                    {
                        void SetField(object v) => current[pair.Key] = v;
                        VisitValue(pair.Value, SetField, processed);
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"The dictionary entry type `{item.GetType().FullName}` is not supported.");
                    }
                }
            }
            else
            {
                foreach (var property in GetProperties(obj))
                {
                    var name = property.GetGraphQLName();
                    var value = property.GetValue(obj);
                    void SetField(object v) => current[name] = v;
                    VisitValue(value, SetField, processed);
                }
            }

            processed.Remove(obj);
        }
        else
        {
            throw new GraphQLException(
                TypeResources.ObjectToDictionaryConverter_CycleInObjectGraph);
        }
    }

    private void VisitList(
        ICollection list,
        Action<object> setValue,
        HashSet<object> processed)
    {
        var valueList = new List<object>();
        setValue(valueList);

        void AddItem(object item) => valueList.Add(item);

        foreach (var element in list)
        {
            VisitValue(element, AddItem, processed);
        }
    }

    private ReadOnlySpan<PropertyInfo> GetProperties(object value)
    {
        var type = value.GetType();

        if (!_properties.TryGetValue(type, out var properties))
        {
            properties = ReflectionUtils.GetProperties(type).Values.ToArray();
            _properties.TryAdd(type, properties);
        }

        return properties;
    }
}
