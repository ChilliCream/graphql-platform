using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    public static class ValueDeserializer
    {
        public static T ParseLiteral<T>(
           IInputType sourceType,
           IValueNode literal)
        {
            return (T)ParseLiteral(sourceType, typeof(T), literal);
        }

        public static object ParseLiteral(
            IInputType sourceType,
            Type targetType,
            IValueNode literal)
        {
            if (sourceType.IsScalarType() || sourceType.IsEnumType())
            {
                return ParseScalarType(sourceType, targetType, literal);
            }
            else if (sourceType.IsListType())
            {
                return ParseListType(sourceType, targetType, literal);
            }
            else if (sourceType.IsInputObjectType()
                && sourceType.NamedType() is InputObjectType iot)
            {
                return ParseObjectType(iot, targetType, literal);
            }

            throw new NotSupportedException(
                "The serializer does not support the specified source type.");
        }

        private static object ParseScalarType(
            IInputType sourceType,
            Type targetType,
            IValueNode literal)
        {
            object value = sourceType.ParseLiteral(literal.ValueOrNullValue());

            if (!sourceType.ClrType.IsAssignableFrom(targetType))
            {
                value = Convert.ChangeType(value, targetType);
            }

            return value;
        }

        private static object ParseListType(
            IInputType sourceType,
            Type targetType,
            IValueNode literal)
        {
            var sourceElementType = (IInputType)sourceType.ElementType();
            Type targetElementType = GetListElementType(targetType);
            List<object> items = new List<object>();

            if (literal is ListValueNode lv)
            {
                foreach (IValueNode element in lv.Items)
                {
                    items.Add(ParseLiteral(
                        sourceElementType, targetElementType,
                        element));
                }
            }
            else
            {
                items.Add(ParseLiteral(
                    sourceElementType, targetElementType,
                    literal));
            }

            return CreateListObject(targetType, targetElementType, items);
        }

        private static Type GetListElementType(Type listType)
        {
            if (listType.IsArray)
            {
                return listType.GetElementType();
            }
            else if (listType.IsInterface && listType.IsGenericType)
            {
                return GetListElementTypeFromInterface(listType);
            }
            else if (listType.IsClass)
            {
                return GetListElementTypeFromClass(listType);
            }

            throw new NotSupportedException(
                "List interface type is not supported.");
        }

        private static Type GetListElementTypeFromInterface(Type listType)
        {
            Type typeDefinition = listType.GetGenericTypeDefinition();

            if (typeof(IList<>) == typeDefinition
                || typeof(ICollection<>) == typeDefinition
                || typeof(IEnumerable<>) == typeDefinition
                || typeof(IReadOnlyList<>) == typeDefinition
                || typeof(IReadOnlyCollection<>) == typeDefinition)
            {
                return listType.GetGenericArguments().Single();
            }

            throw new NotSupportedException(
                "List interface type is not supported.");
        }

        private static Type GetListElementTypeFromClass(Type listType)
        {
            Type typeDefinition = listType.GetGenericTypeDefinition();

            if (typeof(List<>) == typeDefinition
                || typeof(Collection<>) == typeDefinition
                || typeof(HashSet<>) == typeDefinition)
            {
                return listType.GetGenericArguments().Single();
            }

            throw new NotSupportedException(
                "List class type is not supported.");
        }

        private static object CreateListObject(
            Type listType,
            Type elementType,
            List<object> items)
        {
            if (listType.IsArray)
            {
                return CreateArray(elementType, items);
            }
            else if (listType.IsInterface && listType.IsGenericType)
            {
                return CreateListForInterface(elementType, items);
            }
            else if (listType.IsClass)
            {
                return CreateList(listType, elementType, items);
            }

            throw new NotSupportedException(
                "List interface type is not supported.");
        }

        private static object CreateArray(
            Type elementType,
            List<object> items)
        {
            Array array = Array.CreateInstance(elementType, items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                array.SetValue(items[i], i);
            }

            return array;
        }

        private static object CreateListForInterface(
            Type elementType,
            List<object> items)
        {
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType);

            for (int i = 0; i < items.Count; i++)
            {
                list.Add(items[i]);
            }

            return list;
        }

        private static object CreateList(
            Type listType,
            Type elementType,
            List<object> items)
        {
            if (typeof(IList).IsAssignableFrom(listType))
            {
                IList list = (IList)Activator.CreateInstance(listType);

                for (int i = 0; i < items.Count; i++)
                {
                    list.Add(items[i]);
                }

                return list;
            }

            if (listType.IsGenericType
                && listType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                Type enumType = typeof(IEnumerable<>)
                    .MakeGenericType(elementType);
                ConstructorInfo constructor =
                    listType.GetConstructor(new[] { enumType });

                object array = CreateArray(elementType, items);
                return constructor.Invoke(new[] { array });
            }

            throw new NotSupportedException(
                "The specified type is not supported as a list type.");
        }

        private static object ParseObjectType(
            InputObjectType sourceType,
            Type targetType,
            IValueNode literal)
        {
            if (literal.IsNull())
            {
                return null;
            }

            Dictionary<string, IValueNode> fieldValues =
                ((ObjectValueNode)literal).Fields
                    .ToDictionary(t => t.Name.Value, t => t.Value);

            ILookup<string, PropertyInfo> properties =
                targetType.GetProperties()
                    .ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

            object obj = Activator.CreateInstance(targetType);

            foreach (InputField field in sourceType.Fields)
            {
                PropertyInfo property = properties[field.Name].FirstOrDefault();
                if (property != null)
                {
                    SetProperty(field, fieldValues, obj, property);
                }
            }

            return obj;
        }

        private static void SetProperty(
            InputField field,
            Dictionary<string, IValueNode> fieldValues,
            object obj,
            PropertyInfo property)
        {
            SetProperty(field, fieldValues, obj, property,
                l => ParseLiteral(field.Type, property.PropertyType, l));
        }

        internal static void SetProperty(
            InputField field,
            Dictionary<string, IValueNode> fieldValues,
            object obj,
            PropertyInfo property,
            Func<IValueNode, object> parseLiteral)
        {
            if (!fieldValues.TryGetValue(field.Name, out IValueNode literal))
            {
                literal = field.DefaultValue.ValueOrNullValue();
            }

            if (!field.Type.IsNonNullType() || !literal.IsNull())
            {
                object parsedValue = parseLiteral(literal);

                SetProperty(
                    property, field.Type.IsListType(),
                    obj, parsedValue);
            }
        }

        internal static void SetProperty(
            PropertyInfo property,
            bool isListType,
            object obj,
            object value)
        {
            if (property.CanWrite)
            {
                property.SetValue(obj, value);
            }
            else if (isListType
                && typeof(IList).IsAssignableFrom(property.PropertyType))
            {
                IList list = (IList)property.GetValue(obj);
                foreach (object element in (IEnumerable)value)
                {
                    list.Add(element);
                }
            }
        }

        private static IValueNode ValueOrNullValue(this IValueNode literal)
        {
            return literal ?? NullValueNode.Default;
        }
    }
}
