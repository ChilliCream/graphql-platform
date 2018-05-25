using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    internal static class InputObjectDefaultDeserializer
    {
        public static object ParseLiteral(
            SchemaContext schemaContext,
            InputObjectType inputObjectType,
            ObjectValueNode literal)
        {
            Dictionary<string, IValueNode> fieldValues = literal.Fields
                .ToDictionary(t => t.Name.Value, t => t.Value);

            Dictionary<string, PropertyInfo> properties = schemaContext
                .GetNativeTypeMembers(inputObjectType.Name)
                .ToDictionary(t => t.FieldName, t => (PropertyInfo)t.Member);

            object nativeInputObject = Activator.CreateInstance(
                inputObjectType.NativeType);

            foreach (InputField field in inputObjectType.Fields.Values)
            {
                if (fieldValues.TryGetValue(field.Name, out IValueNode value))
                {
                    DeserializeProperty(properties, field, literal, nativeInputObject);
                }
                else if (field.DefaultValue != null)
                {
                    if (field.DefaultValue is NullValueNode && field.Type.IsNonNullType())
                    {
                        // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                    }
                    DeserializeProperty(properties, field, literal, nativeInputObject);
                }
                else if (field.Type.IsNonNullType())
                {
                    // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                }
            }

            return nativeInputObject;
        }

        private static void DeserializeProperty(
            Dictionary<string, PropertyInfo> properties,
            InputField field,
            IValueNode literal,
            object nativeInputObject)
        {
            if (properties.TryGetValue(field.Name, out PropertyInfo property))
            {
                if (property.PropertyType.IsAssignableFrom(field.Type.NativeType))
                {
                    property.SetValue(nativeInputObject, field.Type.ParseLiteral(literal));
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
}
