using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Prometheus.Abstractions;

namespace Prometheus.Resolvers
{
    internal class InputObjectValueConverter
        : IValueConverter
    {
        public object Convert(IValue value, Type desiredType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is InputObjectValue iov)
            {
                if (desiredType == typeof(object)
                    || typeof(IDictionary<string, object>).IsAssignableFrom(desiredType))
                {
                    return DeserializeToDictionary(iov.Fields);
                }

                if (desiredType == typeof(string))
                {
                    Dictionary<string, object> obj = DeserializeToDictionary(iov.Fields);
                    return JsonConvert.SerializeObject(obj);
                }

                return DeserializeToObject(iov.Fields, desiredType);
            }

            throw new ArgumentException(
                "The specified type is not an input object type.",
                nameof(value));
        }

        private Dictionary<string, object> DeserializeToDictionary(
            IEnumerable<KeyValuePair<string, IValue>> fields)
        {
            Dictionary<string, object> inputObject = new Dictionary<string, object>();
            foreach (KeyValuePair<string, IValue> field in fields)
            {
                inputObject[field.Key] = ValueConverter
                    .Convert(field.Value, GetDesiredPropertyType(field.Value));
            }
            return inputObject;
        }

        private Type GetDesiredPropertyType(IValue value)
        {
            if (value is InputObjectValue)
            {
                return typeof(IDictionary<string, object>);
            }

            if (value is ListValue)
            {
                return typeof(IList<object>);
            }

            return typeof(object);
        }

        private object DeserializeToObject(
            IEnumerable<KeyValuePair<string, IValue>> fields,
            Type desiredType)
        {
            object obj = Activator.CreateInstance(desiredType);
            ILookup<string, PropertyInfo> properties = desiredType.GetProperties()
                .ToLookup(t => ReflectionHelper.GetMemberName(t),
                    StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, IValue> field in fields)
            {
                PropertyInfo property = properties[field.Key].FirstOrDefault();
                if (property != null)
                {
                    property.SetValue(obj, ValueConverter.Convert(
                        field.Value, property.PropertyType));
                }
            }

            return obj;
        }

        public IValue Convert(object value, ISchemaDocument schema, IType desiredType)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (desiredType == null)
            {
                throw new ArgumentNullException(nameof(desiredType));
            }

            if (value == null)
            {
                return NullValue.Instance;
            }

            if (!schema.InputObjectTypes.TryGetValue(
                desiredType.TypeName(), out var typeDefinition))
            {
                throw new GraphQLQueryException(
                    "The specified desired type is not a input object type.");
            }

            if (value is IDictionary<string, object> d)
            {
                return CreateInputObject(d, schema, typeDefinition);
            }
            return CreateInputObject(value, schema, typeDefinition);
        }

        private InputObjectValue CreateInputObject(
            IDictionary<string, object> value,
            ISchemaDocument schema,
            InputObjectTypeDefinition typeDefinition)
        {
            Type objectType = value.GetType();

            Dictionary<string, IValue> fields = new Dictionary<string, IValue>();

            foreach (InputValueDefinition field in typeDefinition.Fields.Values)
            {
                if (!value.TryGetValue(field.Name, out var fieldValue))
                {
                    throw new GraphQLQueryException(
                        "The specified input object is missing a required field.");
                }

                fields[field.Name] = ValueConverter.Convert(
                    fieldValue, schema, field.Type);
            }

            return new InputObjectValue(fields);
        }

        private InputObjectValue CreateInputObject(object value,
            ISchemaDocument schema,
            InputObjectTypeDefinition typeDefinition)
        {
            Type objectType = value.GetType();
            ILookup<string, PropertyInfo> properties = objectType.GetProperties()
                .ToLookup(t => ReflectionHelper.GetMemberName(t),
                   StringComparer.OrdinalIgnoreCase);

            Dictionary<string, IValue> fields = new Dictionary<string, IValue>();

            foreach (InputValueDefinition field in typeDefinition.Fields.Values)
            {
                PropertyInfo property = properties[field.Name].FirstOrDefault();
                if (property == null)
                {
                    throw new GraphQLQueryException(
                        "The specified input object is missing a required field.");
                }

                fields[field.Name] = ValueConverter.Convert(
                    property.GetValue(value), schema, field.Type);
            }

            return new InputObjectValue(fields);
        }
    }
}