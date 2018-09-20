using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal static class InputObjectDefaultDeserializer
    {
        public static object ParseLiteral(
           InputObjectType inputObjectType,
           ObjectValueNode literal)
        {
            return ParseLiteral(
                inputObjectType,
                inputObjectType.ClrType,
                literal);
        }

        public static object ParseLiteral(
            InputObjectType inputObjectType,
            Type clrType,
            ObjectValueNode literal)
        {
            var fieldValues = literal.Fields
                .ToDictionary(t => t.Name.Value, t => t.Value);

            object nativeInputObject = Activator.CreateInstance(clrType);

            foreach (InputField field in inputObjectType.Fields)
            {
                if (fieldValues.TryGetValue(field.Name, out IValueNode value))
                {
                    DeserializeProperty(
                        field.Property, field,
                        value, nativeInputObject);
                }
                else if (field.DefaultValue != null)
                {
                    if (field.DefaultValue is NullValueNode
                        && field.Type.IsNonNullType())
                    {
                        // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                    }
                    DeserializeProperty(
                        field.Property, field,
                        value, nativeInputObject);
                }
                else if (field.Type.IsNonNullType())
                {
                    // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                }
            }

            return nativeInputObject;
        }

        private static void DeserializeProperty(
            PropertyInfo property,
            InputField field,
            IValueNode literal,
            object nativeInputObject)
        {
            if (property != null)
            {
                if (property.PropertyType.IsAssignableFrom(field.Type.ClrType))
                {
                    property.SetValue(
                        nativeInputObject,
                        field.Type.ParseLiteral(
                        literal ?? NullValueNode.Default));
                }
                else
                {
                    // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                }
            }
            else
            {
                // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
            }
        }
    }

    internal static class InputObjectDeserializer
    {
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
            if (!fieldValues.TryGetValue(field.Name, out IValueNode value))
            {
                value = field.DefaultValue.ValueOrNullValue();
            }

            if (!field.Type.IsNonNullType() || !value.IsNull())
            {
                object parsedValue = ParseLiteral(
                    field.Type, property.PropertyType,
                    value);

                if (property.CanWrite)
                {
                    property.SetValue(obj, parsedValue);
                }
                else if (field.Type.IsListType()
                    && typeof(IList).IsAssignableFrom(property.PropertyType))
                {
                    IList list = (IList)property.GetValue(obj);
                    foreach (object element in (IEnumerable)parsedValue)
                    {
                        list.Add(element);
                    }
                }
            }
        }

        private static IValueNode ValueOrNullValue(this IValueNode literal)
        {
            return literal ?? NullValueNode.Default;
        }
    }
}
