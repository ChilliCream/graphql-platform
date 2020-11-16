using System;
using System.Collections;
using System.Collections .Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Utilities
{
    internal sealed class ListTypeConverter : IChangeTypeProvider
    {
        private static readonly MethodInfo _dictionaryConvert =
            typeof(ListTypeConverter).GetMethod(
                "GenericDictionaryConverter",
                BindingFlags.Static | BindingFlags.NonPublic)!;

        public bool TryCreateConverter(
            Type source,
            Type target,
            ChangeTypeProvider root,
            [NotNullWhen(true)] out ChangeType? converter)
        {
            Type? sourceElement = ExtendedType.Tools.GetElementType(source);
            Type? targetElement = ExtendedType.Tools.GetElementType(target);

            if (sourceElement is not null
                && targetElement is not null
                && root(sourceElement, targetElement, out ChangeType? elementConverter))
            {
                if (target.IsArray)
                {
                    converter = input => GenericArrayConverter(
                        (ICollection?)input, targetElement, elementConverter);
                    return true;
                }

                if (target.IsGenericType
                    && target.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    MethodInfo converterMethod =
                        _dictionaryConvert.MakeGenericMethod(targetElement.GetGenericArguments());
                    converter = source => converterMethod.Invoke(
                        null, new[] { source, elementConverter });
                    return true;
                }

                if (target.IsGenericType
                    && target.IsInterface)
                {
                    Type listType = typeof(List<>).MakeGenericType(targetElement);
                    if (target.IsAssignableFrom(listType))
                    {
                        converter = source => GenericListConverter(
                            (ICollection?)source, listType, elementConverter);
                        return true;
                    }
                }

                if (target.IsGenericType
                    && target.IsClass
                    && typeof(ICollection).IsAssignableFrom(target))
                {
                    converter = source => GenericListConverter(
                        (ICollection?)source, target, elementConverter);
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
            int i = 0;
            foreach (object? item in source)
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
            ChangeListType(
                input,
                (item, index) => array.SetValue(elementConverter(item), index));
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
            ChangeListType(input, (item, index) => list.Add(elementConverter(item)));
            return list;
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
                (item, index) => list.Add((KeyValuePair<TKey, TValue>)elementConverter(item)!));
            return list;
        }
    }
}
