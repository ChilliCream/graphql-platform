using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Types
{
    public class InputSerializer
    {
        private readonly ITypeConverter _converter;
        private readonly IErrorHandler _errorHandler;

        public InputSerializer(ITypeConverter converter, IErrorHandler errorHandler)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        public object? Deserialize(object? resultValue, IType type, Path path)
        {
            if (resultValue is null)
            {
                if (type.Kind == TypeKind.NonNull)
                {
                    throw new SerializationException("", type, path);
                }

                return null;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    return Deserialize(resultValue, ((NonNullType)type).Type, path);

                case TypeKind.List:
                    return Deserialize((IList)resultValue, (ListType)type, path);

                case TypeKind.InputObject:
                    return Deserialize(
                        (IReadOnlyDictionary<string, object?>)resultValue,
                        (InputObjectType)type,
                        path);

                case TypeKind.Enum:
                case TypeKind.Scalar:


                default:
                    throw new NotSupportedException();
            }
        }

        private List<object?> Deserialize(IList resultValue, ListType type, Path path)
        {
            var list = new List<object?>();

            for (var i = 0; i < resultValue.Count; i++)
            {
                list.Add(Deserialize(resultValue[i], type.ElementType, path.Append(i)));
            }

            return list;
        }

        private object Deserialize(
            IReadOnlyDictionary<string, object?> resultValue,
            InputObjectType type,
            Path path)
        {
            var fieldValues = new object[type.Fields.Count];
            var consumed = 0;

            for (var i = 0; i < type.Fields.Count; i++)
            {
                InputField field = type.Fields[i];

                if (resultValue.TryGetValue(field.Name.Value, out var fieldValue))
                {
                    object value = Deserialize(fieldValue, field.Type, path.Append(field.Name));
                    value = field.Formatter?.OnAfterDeserialize(value) ?? value;
                    fieldValues[i] = ConvertValue(field, value);
                    consumed++;
                }
                else if(!field.IsOptional)
                {
                    object value = field.Type.ParseLiteral(field.DefaultValue, false);
                    fieldValues[i] = ConvertValue(field, value);
                }
            }

            if (consumed < resultValue.Count)
            {
                var invalidFieldNames = new List<string>();

                foreach (string key in resultValue.Keys)
                {
                    if (!type.Fields.ContainsField(key))
                    {
                        invalidFieldNames.Add(key);
                    }
                }

                if (invalidFieldNames.Count == 1)
                {
                    // TODO : Resources
                    throw new SerializationException(
                        string.Format(
                            "The field `{0}` does not exist on the type `{1}`.",
                            invalidFieldNames[0],
                            type.Name.Value),
                        type);
                }

                throw new SerializationException(
                    string.Format(
                        "The fields `{0}` do not exist on the type `{1}`.",
                        string.Join(", ", invalidFieldNames.Select(t => $"`{t}`")),
                        type.Name.Value),
                    type);
            }

            return type.CreateInstance(fieldValues);
        }

        private object Deserialize(object resultValue, ILeafType type, Path path)
        {
            try
            {
                return type.Deserialize(resultValue);
            }
            catch (SerializationException ex)
            {
                throw new SerializationException(ex.Errors[0], ex.Type, path);
            }
        }

        private object? ConvertValue(InputField field, object? value)
        {
            if (value is null)
            {
                return null;
            }

            Type valueType = value.GetType();
            Type type = field.RuntimeType;

            if (valueType == type)
            {
                return value;
            }

            if (field.RuntimeType != typeof(object))
            {
                if (type.IsInterface && type.IsInstanceOfType(value))
                {
                    return value;
                }

                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                value = _converter.Convert(value.GetType(), type, value);
            }

            return value;
        }
    }
}
