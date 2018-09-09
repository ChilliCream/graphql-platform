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
            InputObjectType inputObjectType,
            ObjectValueNode literal)
        {
            Dictionary<string, IValueNode> fieldValues = literal.Fields
                .ToDictionary(t => t.Name.Value, t => t.Value);

            object nativeInputObject = Activator.CreateInstance(
                inputObjectType.NativeType);

            foreach (InputField field in inputObjectType.Fields)
            {
                if (fieldValues.TryGetValue(field.Name, out IValueNode value))
                {
                    DeserializeProperty(field.Property, field, value, nativeInputObject);
                }
                else if (field.DefaultValue != null)
                {
                    if (field.DefaultValue is NullValueNode && field.Type.IsNonNullType())
                    {
                        // TODO : thorw type deserialization exception -> InputObjectTypeDeserializationException
                    }
                    DeserializeProperty(field.Property, field, value, nativeInputObject);
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
                if (property.PropertyType.IsAssignableFrom(field.Type.NativeType))
                {
                    property.SetValue(nativeInputObject, field.Type.ParseLiteral(
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
}
