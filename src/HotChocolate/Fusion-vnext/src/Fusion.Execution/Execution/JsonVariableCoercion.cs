using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal ref struct JsonVariableCoercion
{
    private const int MaxAllowedDepth = 64;
    private readonly IFeatureProvider _context;
    private readonly ref Utf8MemoryBuilder? _memory;

    public JsonVariableCoercion(IFeatureProvider context, ref Utf8MemoryBuilder? memory)
    {
        _context = context;
        _memory = ref memory;
    }

    public bool TryCoerceVariableValue(
        string variableName,
        IInputType variableType,
        JsonElement inputValue,
        [NotNullWhen(true)] out VariableValue? variableValue,
        [NotNullWhen(false)] out IError? error)
    {
        if (inputValue.ValueKind is JsonValueKind.Undefined)
        {
            throw new ArgumentException("Undefined JSON value kind.");
        }

        var root = Path.Root.Append(variableName);

        try
        {
            if (TryParseAndValidate(variableType, inputValue, root, 0, out var valueLiteral, out error))
            {
                variableValue = new VariableValue(variableName, variableType, valueLiteral);
                return true;
            }

            variableValue = null;
            return false;
        }
        catch
        {
            _memory?.Abandon();
            _memory = null;
            throw;
        }
    }

    private bool TryParseAndValidate(
        IInputType type,
        JsonElement element,
        Path path,
        int depth,
        [NotNullWhen(true)] out IValueNode? value,
        [NotNullWhen(false)] out IError? error)
    {
        if (depth > MaxAllowedDepth)
        {
            throw new InvalidOperationException("Max allowed depth reached.");
        }

        // Handle NonNull types
        if (type.Kind is TypeKind.NonNull)
        {
            if (element.ValueKind is JsonValueKind.Null)
            {
                value = null;
                error = ErrorBuilder.New()
                    .SetMessage("The value is not a non-null value.")
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            type = (IInputType)type.InnerType();
        }

        // Handle null values
        if (element.ValueKind is JsonValueKind.Null)
        {
            value = NullValueNode.Default;
            error = null;
            return true;
        }

        // Handle List types
        if (type.Kind is TypeKind.List)
        {
            if (element.ValueKind is not JsonValueKind.Array)
            {
                value = null;
                error = ErrorBuilder.New()
                    .SetMessage("The value is not a list value.")
                    .SetExtension("variable", $"{path}")
                    .Build();
                return false;
            }

            var elementType = (IInputType)type.ListType().ElementType;
            var buffer = ArrayPool<IValueNode>.Shared.Rent(64);
            var count = 0;

            try
            {
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (count == buffer.Length)
                    {
                        var temp = buffer;
                        var tempSpan = temp.AsSpan();
                        buffer = ArrayPool<IValueNode>.Shared.Rent(count * 2);
                        tempSpan.CopyTo(buffer);
                        tempSpan.Clear();
                        ArrayPool<IValueNode>.Shared.Return(temp);
                    }

                    if (!TryParseAndValidate(
                        elementType,
                        item,
                        path.Append(index),
                        depth + 1,
                        out var itemValue,
                        out error))
                    {
                        value = null;
                        return false;
                    }

                    buffer[count++] = itemValue;
                    index++;
                }

                value = new ListValueNode(buffer.AsSpan(0, count).ToArray());
                error = null;
                return true;
            }
            finally
            {
                buffer.AsSpan(0, count).Clear();
                ArrayPool<IValueNode>.Shared.Return(buffer);
            }
        }

        // Handle InputObject types
        if (type.Kind is TypeKind.InputObject)
        {
            return TryParseInputObject(type, element, path, depth, out value, out error);
        }

        // Handle Scalar types
        if (type is FusionScalarTypeDefinition scalarType)
        {
            return TryParseScalar(scalarType, element, path, depth, out value, out error);
        }

        // Handle Enum types
        if (type is FusionEnumTypeDefinition enumType)
        {
            return TryParseEnum(enumType, element, path, out value, out error);
        }

        throw new NotSupportedException(
            $"The type `{type.FullTypeName()}` is not a valid input type.");
    }

    private bool TryParseInputObject(
        IInputType type,
        JsonElement element,
        Path path,
        int depth,
        [NotNullWhen(true)] out IValueNode? value,
        [NotNullWhen(false)] out IError? error)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            value = null;
            error = ErrorBuilder.New()
                .SetMessage("The value is not an object value.")
                .SetExtension("variable", $"{path}")
                .Build();
            return false;
        }

        var inputObjectType = (FusionInputObjectTypeDefinition)type;
        var oneOf = inputObjectType.IsOneOf;

        // Count fields first for OneOf validation
        var fieldCount = 0;
        foreach (var _ in element.EnumerateObject())
        {
            fieldCount++;
        }

        if (oneOf && fieldCount is 0)
        {
            value = null;
            error = ErrorBuilder.New()
                .SetMessage("The OneOf Input Object `{0}` requires that exactly one field is supplied and that field must not be `null`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", inputObjectType.Name)
                .SetCode(ErrorCodes.Execution.OneOfNoFieldSet)
                .SetPath(path)
                .Build();
            return false;
        }

        if (oneOf && fieldCount > 1)
        {
            value = null;
            error = ErrorBuilder.New()
                .SetMessage("More than one field of the OneOf Input Object `{0}` is set. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", inputObjectType.Name)
                .SetCode(ErrorCodes.Execution.OneOfMoreThanOneFieldSet)
                .SetPath(path)
                .Build();
            return false;
        }

        var numberOfInputFields = inputObjectType.Fields.Count;
        var processedCount = 0;
        bool[]? processedBuffer = null;
        var processed = depth <= 256 && numberOfInputFields <= 32
            ? stackalloc bool[numberOfInputFields]
            : processedBuffer = ArrayPool<bool>.Shared.Rent(numberOfInputFields);

        if (processedBuffer is not null)
        {
            processed.Clear();
        }

        var buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(64);
        var count = 0;

        try
        {
            foreach (var property in element.EnumerateObject())
            {
                if (!inputObjectType.Fields.TryGetField(property.Name, out var fieldDefinition))
                {
                    value = null;
                    error = ErrorBuilder.New()
                        .SetMessage(
                            "The field `{0}` is not defined on the input object type `{1}`.",
                            property.Name,
                            inputObjectType.Name)
                        .SetExtension("variable", $"{path}")
                        .Build();
                    return false;
                }

                if (oneOf && property.Value.ValueKind is JsonValueKind.Null)
                {
                    value = null;
                    error = ErrorBuilder.New()
                        .SetMessage("`null` was set to the field `{0}` of the OneOf Input Object `{1}`. OneOf Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null.", property.Name, inputObjectType.Name)
                        .SetCode(ErrorCodes.Execution.OneOfFieldIsNull)
                        .SetPath(path)
                        .SetCoordinate(fieldDefinition.Coordinate)
                        .Build();
                    return false;
                }

                if (count == buffer.Length)
                {
                    var temp = buffer;
                    var tempSpan = temp.AsSpan();
                    buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(count * 2);
                    tempSpan.CopyTo(buffer);
                    tempSpan.Clear();
                    ArrayPool<ObjectFieldNode>.Shared.Return(temp);
                }

                if (!TryParseAndValidate(
                    fieldDefinition.Type,
                    property.Value,
                    path.Append(property.Name),
                    depth + 1,
                    out var fieldValue,
                    out error))
                {
                    value = null;
                    return false;
                }

                buffer[count++] = new ObjectFieldNode(property.Name, fieldValue);
                processed[fieldDefinition.Index] = true;
                processedCount++;
            }

            // Check for missing required fields
            if (!oneOf && processedCount != numberOfInputFields)
            {
                for (var i = 0; i < numberOfInputFields; i++)
                {
                    if (!processed[i])
                    {
                        var field = inputObjectType.Fields[i];

                        if (field.Type.Kind == TypeKind.NonNull && field.DefaultValue is null)
                        {
                            value = null;
                            error = ErrorBuilder.New()
                                .SetMessage("The required input field `{0}` is missing.", field.Name)
                                .SetPath(path.Append(field.Name))
                                .SetExtension("field", field.Coordinate.ToString())
                                .Build();
                            return false;
                        }
                    }
                }
            }

            value = new ObjectValueNode(buffer.AsSpan(0, count).ToArray());
            error = null;
            return true;
        }
        finally
        {
            buffer.AsSpan(0, count).Clear();
            ArrayPool<ObjectFieldNode>.Shared.Return(buffer);

            if (processedBuffer is not null)
            {
                ArrayPool<bool>.Shared.Return(processedBuffer);
            }
        }
    }

    private readonly bool TryParseScalar(
        FusionScalarTypeDefinition scalarType,
        JsonElement element,
        Path path,
        int depth,
        [NotNullWhen(true)] out IValueNode? value,
        [NotNullWhen(false)] out IError? error)
    {
        if (scalarType.IsUpload)
        {
            if (element.ValueKind is JsonValueKind.String
                && element.GetString() is { Length: > 0 } fileKey
                && _context.Features.GetRequired<IFileLookup>().TryGetFile(fileKey, out var file))
            {
                value = new FileReferenceNode(file.OpenReadStream, file.Name, file.ContentType);
                error = null;
                return true;
            }

            error = ErrorBuilder.New()
                .SetMessage("The value is not a valid file.")
                .SetExtension("variable", $"{path}")
                .Build();
            value = null;
            return false;
        }
        else
        {
            value = ParseLiteral(element, depth);

            if (!scalarType.IsValueCompatible(value))
            {
                error = ErrorBuilder.New()
                    .SetMessage(
                        "The value `{0}` is not a valid value for the scalar type `{1}`.",
                        value,
                        scalarType.Name)
                    .SetExtension("variable", $"{path}")
                    .Build();
                value = null;
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryParseEnum(
        FusionEnumTypeDefinition enumType,
        JsonElement element,
        Path path,
        [NotNullWhen(true)] out IValueNode? value,
        [NotNullWhen(false)] out IError? error)
    {
        if (element.ValueKind is not JsonValueKind.String)
        {
            value = null;
            error = ErrorBuilder.New()
                .SetMessage("The value is not an enum value.")
                .SetExtension("variable", $"{path}")
                .Build();
            return false;
        }

        var enumValue = element.GetString()!;

        if (!enumType.Values.ContainsName(enumValue))
        {
            value = null;
            error = ErrorBuilder.New()
                .SetMessage("The value `{0}` is not a valid value for the enum type `{1}`.", enumValue, enumType.Name)
                .SetExtension("variable", $"{path}")
                .Build();
            return false;
        }

        value = new EnumValueNode(enumValue);
        error = null;
        return true;
    }

    private readonly IValueNode ParseLiteral(JsonElement element, int depth)
    {
        if (depth > MaxAllowedDepth)
        {
            throw new InvalidOperationException("Max allowed depth reached.");
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return NullValueNode.Default;

            case JsonValueKind.True:
                return BooleanValueNode.True;

            case JsonValueKind.False:
                return BooleanValueNode.False;

            case JsonValueKind.String:
            {
                var rawValue = element.GetRawText();
                var utf8Value = System.Text.Encoding.UTF8.GetBytes(rawValue);
                var span = utf8Value.AsSpan();
                span = span[1..^1]; // Remove quotes
                var segment = WriteValue(span);
                return new StringValueNode(null, segment, false);
            }

            case JsonValueKind.Number:
            {
                var rawValue = element.GetRawText();
                var utf8Value = System.Text.Encoding.UTF8.GetBytes(rawValue);
                var span = utf8Value.AsSpan();
                var segment = WriteValue(span);

                if (span.IndexOfAny((byte)'e', (byte)'E') > -1)
                {
                    return new FloatValueNode(segment, FloatFormat.Exponential);
                }

                if (span.IndexOf((byte)'.') > -1)
                {
                    return new FloatValueNode(segment, FloatFormat.FixedPoint);
                }

                return new IntValueNode(segment);
            }

            case JsonValueKind.Array:
            {
                var buffer = ArrayPool<IValueNode>.Shared.Rent(64);
                var count = 0;

                try
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        if (count == buffer.Length)
                        {
                            var temp = buffer;
                            var tempSpan = temp.AsSpan();
                            buffer = ArrayPool<IValueNode>.Shared.Rent(count * 2);
                            tempSpan.CopyTo(buffer);
                            tempSpan.Clear();
                            ArrayPool<IValueNode>.Shared.Return(temp);
                        }

                        buffer[count++] = ParseLiteral(item, depth + 1);
                    }

                    return new ListValueNode(buffer.AsSpan(0, count).ToArray());
                }
                finally
                {
                    buffer.AsSpan(0, count).Clear();
                    ArrayPool<IValueNode>.Shared.Return(buffer);
                }
            }

            case JsonValueKind.Object:
            {
                var buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(64);
                var count = 0;

                try
                {
                    foreach (var item in element.EnumerateObject())
                    {
                        if (count == buffer.Length)
                        {
                            var temp = buffer;
                            var tempSpan = temp.AsSpan();
                            buffer = ArrayPool<ObjectFieldNode>.Shared.Rent(count * 2);
                            tempSpan.CopyTo(buffer);
                            tempSpan.Clear();
                            ArrayPool<ObjectFieldNode>.Shared.Return(temp);
                        }

                        buffer[count++] = new ObjectFieldNode(
                            item.Name,
                            ParseLiteral(item.Value, depth + 1));
                    }

                    return new ObjectValueNode(buffer.AsSpan(0, count).ToArray());
                }
                finally
                {
                    buffer.AsSpan(0, count).Clear();
                    ArrayPool<ObjectFieldNode>.Shared.Return(buffer);
                }
            }

            default:
                throw new InvalidOperationException($"Unexpected JSON value kind: {element.ValueKind}");
        }
    }

    private readonly ReadOnlyMemorySegment WriteValue(ReadOnlySpan<byte> value)
    {
        _memory ??= new Utf8MemoryBuilder();
        return _memory.Write(value);
    }
}
