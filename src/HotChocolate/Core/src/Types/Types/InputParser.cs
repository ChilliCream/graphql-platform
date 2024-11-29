#nullable enable

using System.Buffers;
using System.Collections;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

public sealed class InputParser
{
    private static readonly Path _root = Path.Root.Append("root");
    private readonly ITypeConverter _converter;
    private readonly DictionaryToObjectConverter _dictToObjConverter;
    private readonly bool _ignoreAdditionalInputFields;

    public InputParser() : this(new DefaultTypeConverter()) { }

    public InputParser(ITypeConverter converter)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _dictToObjConverter = new DictionaryToObjectConverter(converter);
        _ignoreAdditionalInputFields = false;
    }

    public InputParser(ITypeConverter converter, InputParserOptions options)
    {
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _dictToObjConverter = new DictionaryToObjectConverter(converter);
        _ignoreAdditionalInputFields = options.IgnoreAdditionalInputFields;
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

        var path = Path.Root.Append(field.Name);
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

        return ParseLiteralInternal(value, type, path ?? _root, 0, true, null);
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
            var list = CreateList(type);
            var items = ((ListValueNode)resultValue).Items;
            var flatList = !type.ElementType.IsListType();
            var elementType = type.ElementType;

            if (flatList)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    list.Add(
                        ParseLiteralInternal(
                            items[i],
                            elementType,
                            path.Append(i),
                            stack,
                            defaults,
                            field));
                }
            }
            else
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var itemPath = path.Append(i);

                    if (item.Kind != SyntaxKind.ListValue
                        && item.Kind != SyntaxKind.NullValue)
                    {
                        throw ParseNestedList_InvalidSyntaxKind(type, item.Kind, itemPath);
                    }

                    list.Add(
                        ParseLiteralInternal(
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
            var list = CreateList(type);
            list.Add(
                ParseLiteralInternal(
                    resultValue,
                    type.ElementType,
                    path.Append(0),
                    stack,
                    defaults,
                    field));
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
        if (resultValue.Kind is SyntaxKind.ObjectValue)
        {
            var processedCount = 0;
            bool[]? processedBuffer = null;
            var processed = stack <= 256 && type.Fields.Count <= 32
                ? stackalloc bool[type.Fields.Count]
                : processedBuffer = ArrayPool<bool>.Shared.Rent(type.Fields.Count);

            if (processedBuffer is not null)
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
                var fields = ((ObjectValueNode)resultValue).Fields;
                var oneOf = type.Directives.ContainsDirective(WellKnownDirectives.OneOf);

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
                    var fieldValue = fields[i];

                    if (type.Fields.TryGetField(fieldValue.Name.Value, out var field))
                    {
                        var literal = fieldValue.Value;
                        var fieldPath = path.Append(field.Name);

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
                        invalidFieldNames ??= [];
                        invalidFieldNames.Add(fieldValue.Name.Value);
                    }
                }

                if (!_ignoreAdditionalInputFields && invalidFieldNames?.Count > 0)
                {
                    throw InvalidInputFieldNames(type, invalidFieldNames, path);
                }

                if (processedCount < type.Fields.Count)
                {
                    for (var i = 0; i < type.Fields.Count; i++)
                    {
                        if (!processed[i])
                        {
                            var field = type.Fields[i];
                            var fieldPath = path.Append(field.Name);
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

    private static object? ParseLeaf(
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

            var error = ErrorBuilder.FromError(ex.Errors[0])
                .SetPath(path)
                .SetFieldCoordinate(field.Coordinate)
                .SetExtension("fieldType", type.Name)
                .Build();

            throw new SerializationException(error, ex.Type, path);
        }
    }

    public object ParseDirective(
        DirectiveNode node,
        DirectiveType type,
        Path? path = null)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return ParseDirective(node, type, path ?? Path.Root, 0, true);
    }

    private object ParseDirective(
        DirectiveNode node,
        DirectiveType type,
        Path path,
        int stack,
        bool defaults)
    {
        var processedCount = 0;
        bool[]? processedBuffer = null;
        var processed = stack <= 256 && type.Arguments.Count <= 32
            ? stackalloc bool[type.Arguments.Count]
            : processedBuffer = ArrayPool<bool>.Shared.Rent(type.Arguments.Count);

        if (processedBuffer is not null)
        {
            processed.Clear();
        }

        if (processedBuffer is null)
        {
            stack += type.Arguments.Count;
        }

        var fieldValues = new object?[type.Arguments.Count];
        List<string>? invalidFieldNames = null;

        try
        {
            var fields = node.Arguments;

            for (var i = 0; i < fields.Count; i++)
            {
                var fieldValue = fields[i];

                if (type.Arguments.TryGetField(fieldValue.Name.Value, out var field))
                {
                    var literal = fieldValue.Value;
                    var fieldPath = path.Append(field.Name);

                    if (literal.Kind is SyntaxKind.NullValue &&
                        field.Type.Kind is TypeKind.NonNull)
                    {
                        throw NonNullInputViolation(type, fieldPath, field);
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
                    invalidFieldNames ??= [];
                    invalidFieldNames.Add(fieldValue.Name.Value);
                }
            }

            if (invalidFieldNames?.Count > 0)
            {
                throw InvalidInputFieldNames(type, invalidFieldNames, path);
            }

            if (processedCount < type.Arguments.Count)
            {
                for (var i = 0; i < type.Arguments.Count; i++)
                {
                    if (!processed[i])
                    {
                        var field = type.Arguments[i];
                        var fieldPath = path.Append(field.Name);
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

    public object? ParseResult(object? resultValue, IType type, Path? path = null)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Deserialize(resultValue, type, path ?? _root, null);
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

        if (type.Kind == TypeKind.NonNull)
        {
            type = ((NonNullType)type).Type;
        }

        switch (type.Kind)
        {
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
            var list = CreateList(type);

            for (var i = 0; i < serializedList.Count; i++)
            {
                var newPath = path.Append(i);
                list.Add(Deserialize(serializedList[i], type.ElementType, newPath, field));
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
            var oneOf = type.Directives.ContainsDirective(WellKnownDirectives.OneOf);

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
                var field = type.Fields[i];

                if (map.TryGetValue(field.Name, out var fieldValue))
                {
                    var fieldPath = path.Append(field.Name);

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
                    var fieldPath = path.Append(field.Name);
                    fieldValues[i] = CreateDefaultValue(field, fieldPath, 0);
                }
            }

            if (!_ignoreAdditionalInputFields && consumed < map.Count)
            {
                var invalidFieldNames = new List<string>();

                foreach (var key in map.Keys)
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

            var error = ErrorBuilder.FromError(ex.Errors[0])
                .SetPath(path)
                .SetFieldCoordinate(field.Coordinate)
                .SetExtension("fieldType", type.Name)
                .Build();

            throw new SerializationException(error, ex.Type, path);
        }
    }

    private object? CreateDefaultValue(InputField field, Path path, int stack)
    {
        object? value;

        if (field.DefaultValue is null || field.DefaultValue.Kind == SyntaxKind.NullValue)
        {
            if (field.Type.Kind == TypeKind.NonNull)
            {
                throw RequiredInputFieldIsMissing(field, path);
            }

            value = null;

            // if the type is nullable but the runtime type is a non-nullable value
            // we will create a default instance and assign that instead.
            if (field.RuntimeType.IsValueType)
            {
                value = Activator.CreateInstance(field.RuntimeType);
            }

            return field.IsOptional
                ? new Optional(value, false)
                : value;
        }

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

        return field.IsOptional
            ? new Optional(value, false)
            : value;
    }

    private object? CreateDefaultValue(DirectiveArgument field, Path path, int stack)
    {
        object? value;

        if (field.DefaultValue is null || field.DefaultValue.Kind == SyntaxKind.NullValue)
        {
            if (field.Type.Kind == TypeKind.NonNull)
            {
                throw RequiredInputFieldIsMissing(field, path);
            }

            value = null;

            // if the type is nullable but the runtime type is a non-nullable value
            // we will create a default instance and assign that instead.
            if (field.RuntimeType.IsValueType)
            {
                value = Activator.CreateInstance(field.RuntimeType);
            }

            return field.IsOptional
                ? new Optional(value, false)
                : value;
        }

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

        return field.IsOptional
            ? new Optional(value, false)
            : value;
    }

    private static object? FormatValue(IInputFieldInfo field, object? value)
        => value is null || field.Formatter is null
            ? value
            : field.Formatter.Format(value);

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
