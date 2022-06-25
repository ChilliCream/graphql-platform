#nullable enable

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;
namespace HotChocolate.Types;

public class InputParser
{
    private readonly ITypeConverter _converter;
    private readonly DictionaryToObjectConverter _dictToObjConverter;

    public InputParser() : this(new DefaultTypeConverter()) { }

    public InputParser(ITypeConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _dictToObjConverter = new(converter);
    }

    public object? ParseLiteral(IValueNode value, IInputFieldInfo field, Type? targetType = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        Path path = PathFactory.Instance.New(field.Name);
        var runtimeValue = ParseLiteralInternal(value, field.Type, path, 0, true, field);
        runtimeValue = FormatValue(field, runtimeValue);

        // Caller doesn't care, but to ensure specificity, we set the field's runtime type
        // to make sure it's at least converted to the right type.
        // e.g. from a list to an array if it should be an array
        if (targetType == null || targetType == typeof(object))
        {
            targetType = field.RuntimeType;
        }

        return ConvertValue(targetType, runtimeValue);
    }

    public object? ParseLiteral(IValueNode value, IType type, Path? path = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return ParseLiteralInternal(value, type, path ?? Path.Root, 0, true, null);
    }

    private object? ParseLiteralInternal(
        IValueNode value,
        IType type,
        Path path,
        int stack,
        bool defaults,
        IInputFieldInfo? field)
    {
        if (value.Kind == SyntaxKind.NullValue)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                throw NonNullInputViolation(type, path);
            }

            return null;
        }

        switch (type.Kind)
        {
            case TypeKind.NonNull:
                return ParseLiteralInternal(
                    value,
                    ((NonNullType)type).Type,
                    path,
                    stack,
                    defaults,
                    field);

            case TypeKind.List:
                return ParseList(value, (ListType)type, path, stack, defaults, field);

            case TypeKind.InputObject:
                return ParseObject(value, (InputObjectType)type, path, stack, defaults);

            case TypeKind.Enum:
            case TypeKind.Scalar:
                return ParseLeaf(value, (ILeafType)type, path, field);

            default:
                throw new NotSupportedException();
        }
    }

    private IList ParseList(
        IValueNode resultValue,
        ListType type,
        Path path,
        int stack,
        bool defaults,
        IInputFieldInfo? field)
    {
        if (resultValue.Kind == SyntaxKind.ListValue)
        {
            IList list = CreateList(type);
            IReadOnlyList<IValueNode> items = ((ListValueNode)resultValue).Items;
            var flatList = !type.ElementType.IsListType();
            IType elementType = type.ElementType;

            if (flatList)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    list.Add(ParseLiteralInternal(
                        items[i],
                        elementType,
                        PathFactory.Instance.Append(path, i),
                        stack,
                        defaults,
                        field));
                }
            }
            else
            {
                for (var i = 0; i < items.Count; i++)
                {
                    IValueNode item = items[i];
                    Path itemPath = PathFactory.Instance.Append(path, i);

                    if (item.Kind != SyntaxKind.ListValue)
                    {
                        throw ParseNestedList_InvalidSyntaxKind(type, item.Kind, itemPath);
                    }

                    list.Add(ParseLiteralInternal(
                        item,
                        elementType,
                        itemPath,
                        stack,
                        defaults,
                        field));
                }
            }

            return list;
        }
        else
        {
            IList list = CreateList(type);
            list.Add(ParseLiteralInternal(
                resultValue,
                type.ElementType,
                PathFactory.Instance.Append(path, 0),
                stack,
                defaults, field));
            return list;
        }
    }

    private object ParseObject(
        IValueNode resultValue,
        InputObjectType type,
        Path path,
        int stack,
        bool defaults)
    {
        if (resultValue.Kind == SyntaxKind.ObjectValue)
        {
            var processedCount = 0;
            bool[]? processedBuffer = null;
            Span<bool> processed = stack <= 256 && type.Fields.Count <= 32
                ? stackalloc bool[type.Fields.Count]
                : processedBuffer = ArrayPool<bool>.Shared.Rent(type.Fields.Count);

            if(processedBuffer is not null)
            {
                processed.Clear();
            }

            if (processedBuffer is null)
            {
                stack += type.Fields.Count;
            }

            var fieldValues = new object?[type.Fields.Count];
            List<string>? invalidFieldNames = null;

            try
            {
                IReadOnlyList<ObjectFieldNode> fields = ((ObjectValueNode)resultValue).Fields;
                var oneOf = type.Directives.Contains(WellKnownDirectives.OneOf);

                if (oneOf && fields.Count is 0)
                {
                    throw OneOfNoFieldSet(type, path);
                }

                if (oneOf && fields.Count > 1)
                {
                    throw OneOfMoreThanOneFieldSet(type, path);
                }

                for (var i = 0; i < fields.Count; i++)
                {
                    ObjectFieldNode fieldValue = fields[i];

                    if (type.Fields.TryGetField(fieldValue.Name.Value, out InputField? field))
                    {
                        IValueNode literal = fieldValue.Value;
                        Path fieldPath =
                            PathFactory.Instance.Append(path, field.Name);

                        if (literal.Kind is SyntaxKind.NullValue)
                        {
                            if (field.Type.Kind is TypeKind.NonNull)
                            {
                                throw NonNullInputViolation(type, fieldPath, field);
                            }
                            else if (oneOf)
                            {
                                throw OneOfFieldIsNull(type, fieldPath, field);
                            }
                        }

                        var value = ParseLiteralInternal(
                            literal,
                            field.Type,
                            fieldPath,
                            stack,
                            defaults,
                            field);
                        value = FormatValue(field, value);
                        value = ConvertValue(field.RuntimeType, value);

                        if (field.IsOptional)
                        {
                            value = new Optional(value, true);
                        }

                        fieldValues[field.Index] = value;
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
                            Path fieldPath = PathFactory.Instance.Append(path, field.Name);
                            fieldValues[i] = CreateDefaultValue(field, fieldPath, stack);
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

        throw ParseInputObject_InvalidSyntaxKind(type, resultValue.Kind, path);
    }

    private object? ParseLeaf(
        IValueNode resultValue,
        ILeafType type,
        Path path,
        IInputFieldInfo? field)
    {
        try
        {
            return type.ParseLiteral(resultValue);
        }
        catch (SerializationException ex)
        {
            if (field is null)
            {
                throw new SerializationException(ex.Errors[0].WithPath(path), ex.Type, path);
            }

            IError error = ErrorBuilder.FromError(ex.Errors[0])
                .SetPath(path)
                .SetExtension(nameof(field), field.Coordinate.ToString())
                .SetExtension("fieldType", type.Name.Value)
                .Build();

            throw new SerializationException(error, ex.Type, path);
        }
    }

    public object? ParseResult(object? resultValue, IType type, Path? path = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Deserialize(resultValue, type, path ?? Path.Root, null);
    }

    private object? Deserialize(object? resultValue, IType type, Path path, IInputField? field)
    {
        if (resultValue is null or NullValueNode)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                throw NonNullInputViolation(type, path);
            }

            return null;
        }

        switch (type.Kind)
        {
            case TypeKind.NonNull:
                return Deserialize(resultValue, ((NonNullType)type).Type, path, field);

            case TypeKind.List:
                return DeserializeList(resultValue, (ListType)type, path, field);

            case TypeKind.InputObject:
                return DeserializeObject(
                    resultValue,
                    (InputObjectType)type,
                    path);

            case TypeKind.Enum:
            case TypeKind.Scalar:
                return DeserializeLeaf(resultValue, (ILeafType)type, path, field);

            default:
                throw new NotSupportedException();
        }
    }

    private object DeserializeList(
        object resultValue,
        ListType type,
        Path path,
        IInputField? field)
    {
        if (resultValue is IList serializedList)
        {
            IList list = CreateList(type);

            for (var i = 0; i < serializedList.Count; i++)
            {
                Path newPath = PathFactory.Instance.Append(path, i);
                list.Add(
                    Deserialize(serializedList[i], type.ElementType, newPath, field));
            }

            return list;
        }

        if (resultValue is ListValueNode node)
        {
            return ParseList(node, type, path, 0, true, field);
        }

        throw ParseList_InvalidObjectKind(type, resultValue.GetType(), path);
    }

    private object DeserializeObject(object resultValue, InputObjectType type, Path path)
    {
        if (resultValue is IReadOnlyDictionary<string, object?> map)
        {
            var oneOf = type.Directives.Contains(WellKnownDirectives.OneOf);

            if (oneOf && map.Count is 0)
            {
                throw OneOfNoFieldSet(type, path);
            }

            if (oneOf && map.Count > 1)
            {
                throw OneOfMoreThanOneFieldSet(type, path);
            }

            var fieldValues = new object?[type.Fields.Count];
            var consumed = 0;

            for (var i = 0; i < type.Fields.Count; i++)
            {
                InputField field = type.Fields[i];

                if (map.TryGetValue(field.Name.Value, out var fieldValue))
                {
                    Path fieldPath =
                        PathFactory.Instance.Append(path, field.Name);

                    if (fieldValue is null)
                    {
                        if (field.Type.Kind is TypeKind.NonNull)
                        {
                            throw NonNullInputViolation(type, fieldPath, field);
                        }

                        if (oneOf)
                        {
                            throw OneOfFieldIsNull(type, fieldPath, field);
                        }
                    }

                    var value = Deserialize(fieldValue, field.Type, fieldPath, field);
                    value = FormatValue(field, value);
                    value = ConvertValue(field.RuntimeType, value);

                    if (field.IsOptional)
                    {
                        value = new Optional(value, true);
                    }

                    fieldValues[i] = value;
                    consumed++;
                }
                else
                {
                    Path fieldPath = PathFactory.Instance.Append(path, field.Name);
                    fieldValues[i] = CreateDefaultValue(field, fieldPath, 0);
                }
            }

            if (consumed < map.Count)
            {
                var invalidFieldNames = new List<string>();

                foreach (string key in map.Keys)
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

        if (type.RuntimeType != typeof(object) &&
            type.RuntimeType.IsInstanceOfType(resultValue))
        {
            return resultValue;
        }

        if (resultValue is ObjectValueNode node)
        {
            return ParseObject(node, type, path, 0, true);
        }

        throw ParseInputObject_InvalidObjectKind(type, resultValue.GetType(), path);
    }

    private object? DeserializeLeaf(
        object resultValue,
        ILeafType type,
        Path path,
        IInputField? field)
    {
        if (resultValue is IValueNode node)
        {
            return ParseLeaf(node, type, path, field);
        }

        try
        {
            return type.Deserialize(resultValue);
        }
        catch (SerializationException ex)
        {
            if (field is null)
            {
                throw new SerializationException(ex.Errors[0].WithPath(path), ex.Type, path);
            }

            IError error = ErrorBuilder.FromError(ex.Errors[0])
                .SetPath(path)
                .SetExtension(nameof(field), field.Coordinate.ToString())
                .SetExtension("fieldType", type.Name.Value)
                .Build();

            throw new SerializationException(error, ex.Type, path);
        }
    }

    private object? CreateDefaultValue(InputField field, Path path, int stack)
    {
        if (field.DefaultValue is null || field.DefaultValue.Kind == SyntaxKind.NullValue)
        {
            if (field.Type.Kind == TypeKind.NonNull)
            {
                throw RequiredInputFieldIsMissing(field, path);
            }

            return field.IsOptional ? new Optional(null, false) : null;
        }

        object? value;

        try
        {
            value = ParseLiteralInternal(
                field.DefaultValue,
                field.Type,
                path,
                stack,
                false,
                field);
        }
        catch (SerializationException ex)
        {
            throw new SerializationException(ex.Errors[0].WithPath(path), ex.Type, path);
        }

        value = FormatValue(field, value);
        value = ConvertValue(field.RuntimeType, value);

        return field.IsOptional ? new Optional(value, false) : value;
    }

    private object? FormatValue(IInputFieldInfo field, object? value)
    {
        return field.Formatter is null || value is null
            ? value
            : field.Formatter.OnAfterDeserialize(value);
    }

    private object? ConvertValue(Type requestedType, object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (requestedType.IsInstanceOfType(value))
        {
            return value;
        }

        if (_converter.TryConvert(value.GetType(), requestedType, value, out var converted))
        {
            return converted;
        }

        // create from this the required argument value.
        // This however comes with a performance impact of traversing the dictionary structure
        // and creating from this the object.
        if (value is IReadOnlyDictionary<string, object> or IReadOnlyList<object>)
        {
            return _dictToObjConverter.Convert(value, requestedType);
        }

        return value;
    }

    private static IList CreateList(ListType type)
        => (IList)Activator.CreateInstance(type.ToRuntimeType())!;

    private readonly struct Optional : IOptional
    {
        public Optional(object? value, bool hasValue)
        {
            Value = value;
            HasValue = hasValue;
        }

        public object? Value { get; }

        public bool HasValue { get; }
    }
}
