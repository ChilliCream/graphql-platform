using System.Buffers;
using System.Collections;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

public sealed class InputParser
{
    private static readonly Path s_root = Path.Root.Append("root");
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

    public object? ParseLiteral(
        IValueNode value,
        IInputValueInfo field,
        Type? targetType = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(field);

        var path = Path.Root.Append(field.Name);
        var runtimeValue = ParseLiteralInternal(value, field.Type, path, 0, true, field);

        // Caller doesn't care, but to ensure specificity, we set the field's runtime type
        // to make sure it's at least converted to the right type.
        // e.g. from a list to an array if it should be an array
        if (targetType == null || targetType == typeof(object))
        {
            targetType = field.RuntimeType;
        }

        return FormatAndConvertValue(field, path, value.Location, runtimeValue, false, false, targetType);
    }

    public object? ParseLiteral(IValueNode value, IType type, Path? path = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(type);

        return ParseLiteralInternal(value, type, path ?? s_root, 0, true, null);
    }

    private object? ParseLiteralInternal(
        IValueNode value,
        IType type,
        Path path,
        int stack,
        bool defaults,
        IInputValueInfo? field)
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
                    ((NonNullType)type).NullableType,
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
        IInputValueInfo? field)
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
                var oneOf = type.IsOneOf;

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

                        var value = ParseAndFormatAndConvertLiteral(
                            literal,
                            fieldPath,
                            stack,
                            defaults,
                            field,
                            field.IsOptional,
                            true);
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

    private static object ParseLeaf(
        IValueNode resultValue,
        ILeafType type,
        Path path,
        IInputValueInfo? field)
    {
        try
        {
            return type.CoerceInputLiteral(resultValue);
        }
        catch (LeafCoercionException ex)
        {
            if (field is null)
            {
                throw new LeafCoercionException(ex.Errors[0].WithPath(path), ex.Type, path);
            }

            var error = ErrorBuilder.FromError(ex.Errors[0])
                .SetInputPath(path)
                .SetCoordinate(field.Coordinate)
                .SetExtension("fieldType", type.Name)
                .Build();

            throw new LeafCoercionException(error, ex.Type, path);
        }
    }

    public object ParseDirective(
        DirectiveNode node,
        DirectiveType type,
        Path? path = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(type);

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

                    if (literal.Kind is SyntaxKind.NullValue
                        && field.Type.Kind is TypeKind.NonNull)
                    {
                        throw NonNullInputViolation(type, fieldPath, field);
                    }

                    var value = ParseAndFormatAndConvertLiteral(
                        literal,
                        fieldPath,
                        stack,
                        defaults,
                        field,
                        field.IsOptional,
                        true);
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

    public object? ParseInputValue(JsonElement inputValue, IType type, IFeatureProvider context, Path? path = null)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(context);

        return Deserialize(inputValue, type, path ?? s_root, null, context);
    }

    private object? Deserialize(
        JsonElement inputValue,
        IType type,
        Path path,
        InputField? field,
        IFeatureProvider context)
    {
        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            if (type.Kind == TypeKind.NonNull)
            {
                throw NonNullInputViolation(type, path);
            }

            return null;
        }

        if (type.Kind == TypeKind.NonNull)
        {
            type = ((NonNullType)type).NullableType;
        }

        switch (type.Kind)
        {
            case TypeKind.List:
                return DeserializeList(inputValue, (ListType)type, path, field, context);

            case TypeKind.InputObject:
                return DeserializeObject(inputValue, (InputObjectType)type, path, context);

            case TypeKind.Enum:
            case TypeKind.Scalar:
                return DeserializeLeaf(inputValue, (ILeafType)type, path, field, context);

            default:
                throw new NotSupportedException();
        }
    }

    private object DeserializeList(
        JsonElement inputValue,
        ListType type,
        Path path,
        InputField? field,
        IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.Array)
        {
            var list = CreateList(type);

            var i = 0;
            foreach (var element in inputValue.EnumerateArray())
            {
                var newPath = path.Append(i++);
                list.Add(Deserialize(element, type.ElementType, newPath, field, context));
            }

            return list;
        }

        throw ParseList_InvalidValueKind(type, path);
    }

    private object DeserializeObject(JsonElement inputValue, InputObjectType type, Path path, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.Object)
        {
            var oneOf = type.IsOneOf;
#if NET9_0_OR_GREATER
            var propertyCount = inputValue.GetPropertyCount();
#else
            var propertyCount = inputValue.EnumerateObject().Count();
#endif

            if (oneOf && propertyCount is 0)
            {
                throw OneOfNoFieldSet(type, path);
            }

            if (oneOf && propertyCount > 1)
            {
                throw OneOfMoreThanOneFieldSet(type, path);
            }

            var processedFields = StringSetPool.Shared.Rent();
            List<string>? invalidFieldNames = null;
            var fieldValues = new object?[type.Fields.Count];

            try
            {
                foreach (var property in inputValue.EnumerateObject())
                {
                    if (type.Fields.TryGetField(property.Name, out var field))
                    {
                        var fieldPath = path.Append(field.Name);
                        if (processedFields.Add(property.Name))
                        {
                            if (property.Value.ValueKind is JsonValueKind.Null)
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

                            var value = Deserialize(property.Value, field.Type, fieldPath, field, context);
                            value = FormatAndConvertValue(field, path, null, value, field.IsOptional, true);

                            fieldValues[field.Index] = value;
                        }
                    }
                    else if (!_ignoreAdditionalInputFields)
                    {
                        invalidFieldNames ??= [];
                        invalidFieldNames.Add(property.Name);
                    }
                }

                if (processedFields.Count < fieldValues.Length)
                {
                    for (var i = 0; i < fieldValues.Length; i++)
                    {
                        var field = type.Fields[i];
                        if (processedFields.Add(field.Name))
                        {
                            var fieldPath = path.Append(field.Name);
                            fieldValues[i] = CreateDefaultValue(field, fieldPath, 0);
                        }
                    }
                }
            }
            finally
            {
                StringSetPool.Shared.Return(processedFields);
            }

            if (invalidFieldNames?.Count > 0)
            {
                throw InvalidInputFieldNames(type, invalidFieldNames, path);
            }

            return type.CreateInstance(fieldValues);
        }

        throw ParseInputObject_InvalidValueKind(type, path);
    }

    private static object DeserializeLeaf(
        JsonElement inputValue,
        ILeafType type,
        Path path,
        InputField? field,
        IFeatureProvider context)
    {
        try
        {
            return type.CoerceInputValue(inputValue, context);
        }
        catch (LeafCoercionException ex)
        {
            if (field is null)
            {
                throw new LeafCoercionException(ex.Errors[0].WithPath(path), ex.Type, path);
            }

            var error = ErrorBuilder.FromError(ex.Errors[0])
                .SetInputPath(path)
                .SetCoordinate(field.Coordinate)
                .SetExtension("fieldType", type.Name)
                .Build();

            throw new LeafCoercionException(error, ex.Type, path);
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

            object? value = null;
            return field.IsOptional
                ? new Optional(value, false)
                : value;
        }

        return ParseAndFormatAndConvertLiteral(
            field.DefaultValue,
            path,
            stack,
            false,
            field,
            field.IsOptional,
            false);
    }

    private object? CreateDefaultValue(DirectiveArgument field, Path path, int stack)
    {
        if (field.DefaultValue is null || field.DefaultValue.Kind == SyntaxKind.NullValue)
        {
            if (field.Type.Kind == TypeKind.NonNull)
            {
                throw RequiredInputFieldIsMissing(field, path);
            }

            object? value = null;

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

        return ParseAndFormatAndConvertLiteral(
            field.DefaultValue,
            path,
            stack,
            false,
            field,
            field.IsOptional,
            false);
    }

    private object? ParseAndFormatAndConvertLiteral(
        IValueNode literal,
        Path fieldPath,
        int stack,
        bool defaults,
        IInputValueInfo field,
        bool isOptional,
        bool optionalHasValue)
    {
        var value = ParseLiteralInternal(
            literal,
            field.Type,
            fieldPath,
            stack,
            defaults,
            field);
        return FormatAndConvertValue(field, fieldPath, literal.Location, value, isOptional, optionalHasValue);
    }

    private object? FormatAndConvertValue(
        IInputValueInfo inputValueInfo,
        Path fieldPath,
        Language.Location? location,
        object? value,
        bool isOptional,
        bool optionalHasValue,
        Type? requestedType = null)
    {
        value = FormatValue(inputValueInfo, value);
        value = ConvertValue(requestedType ?? inputValueInfo.RuntimeType, value, out var conversionException);
        if (conversionException != null)
        {
            throw InvalidTypeConversion(inputValueInfo.Type, inputValueInfo, fieldPath, location, conversionException);
        }

        if (isOptional)
        {
            value = new Optional(value, optionalHasValue);
        }

        return value;
    }

    private static object? FormatValue(IInputValueInfo field, object? value)
        => value is null || field.Formatter is null
            ? value
            : field.Formatter.Format(value);

    private object? ConvertValue(Type requestedType, object? value, out Exception? conversionException)
    {
        conversionException = null;
        if (value is null)
        {
            return null;
        }

        if (requestedType.IsInstanceOfType(value))
        {
            return value;
        }

        if (_converter.TryConvert(value.GetType(), requestedType, value, out var converted, out conversionException))
        {
            return converted;
        }

        // Create from this the required argument value.
        // This, however, comes with a performance impact of traversing the dictionary structure
        // and creating from this the object.
        if (conversionException is null
            && value is IReadOnlyDictionary<string, object> or IReadOnlyList<object>)
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
