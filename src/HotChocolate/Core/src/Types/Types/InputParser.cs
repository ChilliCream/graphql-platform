using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

#nullable enable

namespace HotChocolate.Types
{
    public class InputParser
    {
        private readonly ITypeConverter _converter;

        public InputParser(ITypeConverter converter)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public object? ParseLiteral(IValueNode value, IType type, Path path)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return ParseLiteralInternal(value, type, path, 0);
        }

        public object? ParseLiteral(IValueNode value, IInputField type, Path path)
            => throw new NotImplementedException();

        private object? ParseLiteralInternal(IValueNode value, IType type, Path path, int stack)
        {
            if (value.Kind == SyntaxKind.NullValue)
            {
                if (type.Kind == TypeKind.NonNull)
                {
                    // TODO : RESOURCE
                    throw new SerializationException("", type, path);
                }

                return null;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    return ParseLiteralInternal(value, ((NonNullType)type).Type, path, stack);

                case TypeKind.List:
                    return ParseList((ListValueNode)value, (ListType)type, path, stack);

                case TypeKind.InputObject:
                    return ParseObject((ObjectValueNode)value, (InputObjectType)type, path, stack);

                case TypeKind.Enum:
                case TypeKind.Scalar:
                    return ParseLeaf(value, (ILeafType)type, path);

                default:
                    throw new NotSupportedException();
            }
        }

        private List<object?> ParseList(
            ListValueNode resultValue,
            ListType type,
            Path path,
            int stack)
        {
            var list = new List<object?>();
            IReadOnlyList<IValueNode> items = resultValue.Items;

            for (var i = 0; i < items.Count; i++)
            {
                list.Add(ParseLiteralInternal(items[i], type.ElementType, path.Append(i), stack));
            }

            return list;
        }

        private object ParseObject(
            ObjectValueNode resultValue,
            InputObjectType type,
            Path path,
            int stack)
        {
            bool[]? processedBuffer = null;
            Span<bool> processed = stack <= 256 && type.Fields.Count <= 32
                ? stackalloc bool[type.Fields.Count]
                : processedBuffer = ArrayPool<bool>.Shared.Rent(type.Fields.Count);
            var processedCount = 0;

            if (processedBuffer is null)
            {
                stack += type.Fields.Count;
            }

            var fieldValues = new object?[type.Fields.Count];
            List<string>? invalidFieldNames = null;

            try
            {
                for (var i = 0; i < resultValue.Fields.Count; i++)
                {
                    ObjectFieldNode fieldValue = resultValue.Fields[i];

                    if (type.Fields.TryGetField(fieldValue.Name.Value, out InputField? field))
                    {
                        IValueNode literal = fieldValue.Value;
                        Path fieldPath = path.Append(field.Name);

                        if (literal is null || literal.Kind == SyntaxKind.NullValue)
                        {
                            // TODO : RESOURCE
                            throw new SerializationException("", type, fieldPath);
                        }

                        var value = ParseLiteralInternal(literal, field.Type, fieldPath, stack);
                        value = FormatValue(field, value);
                        fieldValues[i] = ConvertValue(field, value);
                        processed[field.Index] = true;
                        processedCount++;
                    }
                    else
                    {
                        invalidFieldNames ??= new List<string>();
                        invalidFieldNames.Add(fieldValue.Name.Value);
                    }
                }

                if (invalidFieldNames?.Count > 0)
                {
                    throw InvalidInputFieldNames(type, invalidFieldNames, path);
                }

                if (processedCount < type.Fields.Count)
                {
                    for (var i = 0; i < type.Fields.Count; i++)
                    {
                        if (!processed[i])
                        {
                            InputField field = type.Fields[i];
                            Path fieldPath = path.Append(field.Name);

                            var value = field.DefaultValue is not null
                                ? CreateDefaultValue(field.DefaultValue, field.Type, path, 0)
                                : null;
                            value = FormatValue(field, value);

                            if (value is null && field.Type.Kind == TypeKind.NonNull)
                            {
                                throw RequiredInputFieldIsMissing(field, fieldPath);
                            }

                            fieldValues[i] = ConvertValue(field, value);
                        }
                    }
                }
            }
            finally
            {
                if (processedBuffer is not null)
                {
                    ArrayPool<bool>.Shared.Return(processedBuffer);
                }
            }

            return type.CreateInstance(fieldValues);
        }

        private object? ParseLeaf(IValueNode resultValue, ILeafType type, Path path)
        {
            try
            {
                return type.ParseLiteral(resultValue);
            }
            catch (SerializationException ex)
            {
                throw new SerializationException(ex.Errors[0], ex.Type, path);
            }
        }

        public object? ParseResult(object? resultValue, IType type, Path path)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return Deserialize(resultValue, type, path);
        }

        private object? Deserialize(object? resultValue, IType type, Path path)
        {
            if (resultValue is null)
            {
                if (type.Kind == TypeKind.NonNull)
                {
                    // TODO : RESOURCE
                    throw new SerializationException("", type, path);
                }

                return null;
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    return Deserialize(resultValue, ((NonNullType)type).Type, path);

                case TypeKind.List:
                    return DeserializeList((IList)resultValue, (ListType)type, path);

                case TypeKind.InputObject:
                    return DeserializeObject(
                        (IReadOnlyDictionary<string, object?>)resultValue,
                        (InputObjectType)type,
                        path);

                case TypeKind.Enum:
                case TypeKind.Scalar:
                    return DeserializeLeaf(resultValue, (ILeafType)type, path);

                default:
                    throw new NotSupportedException();
            }
        }

        private List<object?> DeserializeList(IList resultValue, ListType type, Path path)
        {
            var list = new List<object?>();

            for (var i = 0; i < resultValue.Count; i++)
            {
                list.Add(Deserialize(resultValue[i], type.ElementType, path.Append(i)));
            }

            return list;
        }

        private object DeserializeObject(
            IReadOnlyDictionary<string, object?> resultValue,
            InputObjectType type,
            Path path)
        {
            var fieldValues = new object?[type.Fields.Count];
            var consumed = 0;

            for (var i = 0; i < type.Fields.Count; i++)
            {
                InputField field = type.Fields[i];

                if (resultValue.TryGetValue(field.Name.Value, out var fieldValue))
                {
                    Path fieldPath = path.Append(field.Name);

                    if (fieldValue is null && field.Type.Kind == TypeKind.NonNull)
                    {
                        // TODO : RESOURCE
                        throw new SerializationException("", type, fieldPath);
                    }

                    var value = Deserialize(fieldValue, field.Type, fieldPath);
                    value = FormatValue(field, value);
                    fieldValues[i] = ConvertValue(field, value);
                    consumed++;
                }
                else
                {
                    var value = field.DefaultValue is not null
                        ? CreateDefaultValue(field.DefaultValue, field.Type, path, 0)
                        : null;
                    value = FormatValue(field, value);

                    if (value is null && field.Type.Kind == TypeKind.NonNull)
                    {
                        throw RequiredInputFieldIsMissing(field, path.Append(field.Name));
                    }

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

                throw InvalidInputFieldNames(type, invalidFieldNames, path);
            }

            return type.CreateInstance(fieldValues);
        }

        private object? DeserializeLeaf(object resultValue, ILeafType type, Path path)
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

        private object? CreateDefaultValue(
            IValueNode? defaultValue,
            IType type,
            Path path,
            int stack)
        {
            return null;
        }

        private object? FormatValue(InputField field, object? value)
        {
            return field.Formatter is null || value is null
                ? value
                : field.Formatter.OnAfterDeserialize(value);
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

    public class InputFormatter
    {
        public IValueNode FormatLiteral(object? value, IType type, Path path)
        {
            return default!;
        }

        public object? FormatResult(object? value, IType type, Path path)
        {
            return default!;
        }
    }
}
