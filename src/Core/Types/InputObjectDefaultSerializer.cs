using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal static class InputObjectDefaultSerializer
    {
        public static IValueNode ParseLiteral(
            InputObjectType inputObjectType,
            object obj)
        {
            if (inputObjectType == null)
            {
                throw new ArgumentNullException(nameof(inputObjectType));
            }

            if (obj == null)
            {
                return new NullValueNode();
            }

            return ParseObject(new HashSet<object>(), inputObjectType, obj);
        }

        private static ObjectValueNode ParseObject(
            HashSet<object> processed,
            InputObjectType inputObjectType,
            object obj)
        {
            processed.Add(obj);

            Type type = obj.GetType();
            PropertyInfo[] properties = GetProperties(type);

            Dictionary<string, IValueNode> fieldValues =
                new Dictionary<string, IValueNode>();

            foreach (InputField field in inputObjectType.Fields.Values)
            {
                PropertyInfo property = GetPropertyByName(properties, field.Name);
                if (property != null)
                {
                    HandleFieldValue(processed, fieldValues,
                        field, field.Type, property.GetValue(obj));
                }
            }

            return new ObjectValueNode(fieldValues
                .Select(t => new ObjectFieldNode(t.Key, t.Value))
                .ToList());
        }

        private static void HandleFieldValue(
            HashSet<object> processed,
            Dictionary<string, IValueNode> fieldValues,
            InputField field,
            IInputType fieldType,
            object fieldValue)
        {
            if (fieldValue == null)
            {
                fieldValues[field.Name] = new NullValueNode();
            }
            else if (fieldType.IsNonNullType())
            {
                HandleFieldValue(processed, fieldValues, field,
                    (IInputType)fieldType.InnerType(), fieldValue);
            }
            else if (fieldType.IsListType())
            {
                fieldValues[field.Name] = ParseList(
                    processed, fieldType, fieldValue);
            }
            else if (fieldType.IsScalarType() || fieldType.IsEnumType())
            {
                fieldValues[field.Name] = ParseScalar(
                    fieldType, fieldValue);
            }
            else if (fieldType.IsInputObjectType())
            {
                if (!processed.Contains(fieldValue))
                {
                    fieldValues[field.Name] = ParseObject(
                        processed, (InputObjectType)fieldType, fieldValue);
                }
            }
        }

        private static IValueNode ParseScalar(
            IInputType type, object fieldValue)
        {
            return type.ParseValue(fieldValue);
        }

        private static IValueNode ParseList(
            HashSet<object> processed,
            IInputType type,
            object fieldValue)
        {
            if (fieldValue is IEnumerable enumerable)
            {
                IType elementType = type.ElementType().InnerType();
                if (elementType is InputObjectType iot)
                {
                    return ParseObjectList(processed, iot, enumerable);
                }
                else
                {
                    return ParseScalarList((IInputType)elementType, enumerable);
                }
            }
            return new NullValueNode();
        }

        private static IValueNode ParseScalarList(
            IInputType elementType,
            IEnumerable enumerable)
        {
            List<IValueNode> list = new List<IValueNode>();
            foreach (object element in enumerable)
            {
                list.Add(ParseScalar(elementType, element));
            }
            return new ListValueNode(list);
        }

        private static IValueNode ParseObjectList(
            HashSet<object> processed,
            InputObjectType elementType,
            IEnumerable enumerable)
        {
            List<IValueNode> list = new List<IValueNode>();
            foreach (object element in enumerable)
            {
                if (!processed.Contains(element))
                {
                    list.Add(ParseObject(processed, elementType, element));
                }
            }
            return new ListValueNode(list);
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            return type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance)
                .Where(t => !t.IsSpecialName)
                .ToArray();
        }

        private static PropertyInfo GetPropertyByName(PropertyInfo[] property, string fieldName)
        {
            return property.FirstOrDefault(t =>
                t.GetGraphQLName().Equals(fieldName, StringComparison.Ordinal));
        }
    }
}
