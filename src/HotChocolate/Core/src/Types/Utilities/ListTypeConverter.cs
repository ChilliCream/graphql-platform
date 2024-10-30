using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Utilities;

internal sealed class ListTypeConverter : IChangeTypeProvider
{
    private static readonly MethodInfo _dictionaryConvert =
        typeof(ListTypeConverter).GetMethod(
            nameof(GenericDictionaryConverter),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo _setConvert =
        typeof(ListTypeConverter).GetMethod(
            nameof(HashSetConverter),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo _collectionConvert =
        typeof(ListTypeConverter).GetMethod(
            nameof(GenericCollectionConverter),
            BindingFlags.Static | BindingFlags.NonPublic)!;

    public bool TryCreateConverter(
        Type source,
        Type target,
        ChangeTypeProvider root,
        [NotNullWhen(true)] out ChangeType? converter)
    {
        var sourceElement = ExtendedType.Tools.GetElementType(source);
        var targetElement = ExtendedType.Tools.GetElementType(target);

        if (sourceElement is not null &&
            targetElement is not null &&
            root(sourceElement, targetElement, out var elementConverter))
        {
            if (target.IsArray)
            {
                converter = input => GenericArrayConverter(
                    (ICollection?)input,
                    targetElement,
                    elementConverter);
                return true;
            }

            if (target.IsGenericType
                && (target.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                    || target.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                    || target.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)))
            {
                var converterMethod =
                    _dictionaryConvert.MakeGenericMethod(targetElement.GetGenericArguments());
                converter = s => converterMethod.Invoke(null, [s, elementConverter,]);
                return true;
            }

            if (target is { IsGenericType: true, IsInterface: true, })
            {
                var typeDefinition = target.GetGenericTypeDefinition();

                if (typeDefinition == typeof(ISet<>))
                {
                    var converterMethod = _setConvert.MakeGenericMethod(targetElement);
                    converter = s => converterMethod.Invoke(null, [s, elementConverter,]);
                    return true;
                }

                var listType = typeof(List<>).MakeGenericType(targetElement);

                if (target.IsAssignableFrom(listType))
                {
                    converter = s => GenericListConverter(
                        (ICollection?)s,
                        listType,
                        elementConverter);
                    return true;
                }
            }

            if (target is { IsGenericType: true, IsClass: true, } &&
                typeof(ICollection).IsAssignableFrom(target))
            {
                converter = s => GenericListConverter((ICollection?)s, target, elementConverter);
                return true;
            }

            if (target is { IsGenericType: true, IsClass: true, } &&
                IsGenericCollection(target))
            {
                var converterMethod = _collectionConvert.MakeGenericMethod(targetElement);
                converter = s => converterMethod.Invoke(null, [s, target, elementConverter,]);
                return true;
            }
        }

        converter = null;
        return false;
    }

    private static void ChangeListType(
        IEnumerable source,
        Action<object?, int> addToDestination)
    {
        var i = 0;

        foreach (var item in source)
        {
            addToDestination(item, i++);
        }
    }

    private static object? GenericArrayConverter(
        ICollection? input,
        Type elementType,
        ChangeType elementConverter)
    {
        if (input is null)
        {
            return null;
        }

        var array = Array.CreateInstance(elementType, input.Count);
        ChangeListType(input, (item, index) => array.SetValue(elementConverter(item), index));
        return array;
    }

    private static object? GenericListConverter(
        ICollection? input,
        Type listType,
        ChangeType elementConverter)
    {
        if (input is null)
        {
            return null;
        }

        var list = (IList)Activator.CreateInstance(listType)!;
        ChangeListType(input, (item, _) => list.Add(elementConverter(item)));
        return list;
    }

    private static object? GenericCollectionConverter<T>(
        ICollection? input,
        Type listType,
        ChangeType elementConverter)
    {
        if (input is null)
        {
            return null;
        }

        var collection = (ICollection<T>)Activator.CreateInstance(listType)!;
        ChangeListType(input, (item, _) => collection.Add((T)elementConverter(item)!));
        return collection;
    }

    private static object? GenericDictionaryConverter<TKey, TValue>(
        ICollection? input,
        ChangeType elementConverter)
        where TKey : notnull
    {
        if (input is null)
        {
            return null;
        }

        var list = (ICollection<KeyValuePair<TKey, TValue>>)new Dictionary<TKey, TValue>();
        ChangeListType(
            input,
            (item, _) => list.Add((KeyValuePair<TKey, TValue>)elementConverter(item)!));
        return list;
    }

    private static object? HashSetConverter<TValue>(
        ICollection? input,
        ChangeType elementConverter)
    {
        if (input is null)
        {
            return null;
        }

        var set = new HashSet<TValue>();

        foreach (var value in input)
        {
            set.Add((TValue)elementConverter(value)!);
        }

        return set;
    }

    private static bool IsGenericCollection(Type type)
    {
        var interfaces = type.GetInterfaces();
        ref var start = ref MemoryMarshal.GetArrayDataReference(interfaces);

        for (var i = 0; i < interfaces.Length; i++)
        {
            var interfaceType = Unsafe.Add(ref start, i);

            if (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == typeof(ISet<>))
            {
                return true;
            }
        }

        return false;
    }
}
