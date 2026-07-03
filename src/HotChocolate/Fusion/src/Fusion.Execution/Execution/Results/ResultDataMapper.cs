using System.Text.Json;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Types;
using JsonWriter = HotChocolate.Text.Json.JsonWriter;

namespace HotChocolate.Fusion.Execution.Results;

internal static class ResultDataMapper
{
    /// <summary>
    /// Maps a value selection from the composite result and writes it directly as JSON.
    /// Returns <c>true</c> if the value was written successfully, <c>false</c> if the
    /// value could not be resolved (undefined/null for required paths) or a null value
    /// landed in a non-null input position of <paramref name="inputType"/>.
    /// </summary>
    public static bool TryMap(
        CompositeResultElement result,
        IValueSelectionNode valueSelection,
        IType? inputType,
        ISchemaDefinition schema,
        JsonWriter writer)
        => Visit(valueSelection, result, inputType, schema, writer);

    private static bool Visit(
        IValueSelectionNode node,
        CompositeResultElement result,
        IType? type,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                return VisitChoice(choice, result, type, schema, writer);

            case PathNode path:
                return VisitPath(path, result, type, schema, writer);

            case ObjectValueSelectionNode objectValue:
                return VisitObject(objectValue, result, type, schema, writer);

            case PathObjectValueSelectionNode pathObject:
                return VisitPathObject(pathObject, result, type, schema, writer);

            case PathListValueSelectionNode pathList:
                return VisitPathList(pathList, result, type, schema, writer);

            default:
                throw new NotSupportedException("Unknown value selection node type.");
        }
    }

    private static bool VisitChoice(
        ChoiceValueSelectionNode node,
        CompositeResultElement result,
        IType? type,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        foreach (var branch in node.Branches)
        {
            if (Visit(branch, result, type, schema, writer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool VisitPath(
        PathNode node,
        CompositeResultElement result,
        IType? type,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        var resolved = ResolvePath(schema, result, node);
        var valueKind = resolved.ValueKind;

        if (valueKind is JsonValueKind.Undefined)
        {
            return false;
        }

        if (valueKind is JsonValueKind.Null)
        {
            if (IsNonNullPosition(type))
            {
                return false;
            }

            writer.WriteNullValue();
            return true;
        }

        if (valueKind is JsonValueKind.Array)
        {
            return TryWriteLeafArray(resolved, GetElementType(type), writer);
        }

        WriteLeafValue(resolved, valueKind, writer);
        return true;
    }

    private static bool TryWriteLeafArray(
        CompositeResultElement array,
        IType? elementType,
        JsonWriter writer)
    {
        writer.WriteStartArray();

        foreach (var item in array.EnumerateArray())
        {
            var itemKind = item.ValueKind;

            if (itemKind is JsonValueKind.Null)
            {
                if (IsNonNullPosition(elementType))
                {
                    return false;
                }

                writer.WriteNullValue();
            }
            else if (itemKind is JsonValueKind.Array)
            {
                if (!TryWriteLeafArray(item, GetElementType(elementType), writer))
                {
                    return false;
                }
            }
            else
            {
                WriteLeafValue(item, itemKind, writer);
            }
        }

        writer.WriteEndArray();
        return true;
    }

    private static void WriteLeafValue(
        CompositeResultElement value,
        JsonValueKind valueKind,
        JsonWriter writer)
    {
        // A custom scalar can have a JSON object as its runtime value, which
        // only the backing document can serialize.
        if (valueKind is JsonValueKind.Object)
        {
            value.WriteTo(writer);
            return;
        }

        writer.WriteRawValue(value.GetRawValue(includeQuotes: true));
    }

    private static bool VisitObject(
        ObjectValueSelectionNode node,
        CompositeResultElement result,
        IType? type,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        if (result.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        var inputObjectType = GetInputObjectType(type);

        writer.WriteStartObject();

        foreach (var field in node.Fields)
        {
            writer.WritePropertyName(field.Name.Value);

            IType? fieldType = null;

            if (inputObjectType is not null
                && inputObjectType.Fields.TryGetField(field.Name.Value, out var inputField))
            {
                fieldType = inputField.Type;
            }

            bool written;

            if (field.ValueSelection is null)
            {
                var pathNode = new PathNode(new PathSegmentNode(field.Name));
                written = VisitPath(pathNode, result, fieldType, schema, writer);
            }
            else
            {
                written = Visit(field.ValueSelection, result, fieldType, schema, writer);
            }

            if (!written)
            {
                return false;
            }
        }

        writer.WriteEndObject();
        return true;
    }

    private static bool VisitPathObject(
        PathObjectValueSelectionNode node,
        CompositeResultElement result,
        IType? type,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        var resolved = ResolvePath(schema, result, node.Path);
        var valueKind = resolved.ValueKind;

        if (valueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        if (valueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        return VisitObject(node.ObjectValueSelection, resolved, type, schema, writer);
    }

    private static bool VisitPathList(
        PathListValueSelectionNode node,
        CompositeResultElement result,
        IType? type,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        var resolved = ResolvePath(schema, result, node.Path);
        var valueKind = resolved.ValueKind;

        switch (valueKind)
        {
            case JsonValueKind.Undefined:
                return false;

            case JsonValueKind.Null:
                if (IsNonNullPosition(type))
                {
                    return false;
                }

                writer.WriteNullValue();
                return true;

            case JsonValueKind.Array:
                return VisitList(node.ListValueSelection, resolved, GetElementType(type), schema, writer);

            default:
                return false;
        }
    }

    private static bool VisitList(
        ListValueSelectionNode node,
        CompositeResultElement result,
        IType? elementType,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        if (result.ValueKind is not JsonValueKind.Array)
        {
            return false;
        }

        writer.WriteStartArray();

        foreach (var item in result.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Null)
            {
                if (IsNonNullPosition(elementType))
                {
                    return false;
                }

                writer.WriteNullValue();
                continue;
            }

            if (!Visit(node.ElementSelection, item, elementType, schema, writer))
            {
                return false;
            }
        }

        writer.WriteEndArray();
        return true;
    }

    private static bool IsNonNullPosition(IType? type)
        => type?.Kind is TypeKind.NonNull;

    private static IType? GetElementType(IType? type)
    {
        if (type is NonNullType nonNullType)
        {
            type = nonNullType.NullableType;
        }

        return type is ListType listType ? listType.ElementType : null;
    }

    private static IInputObjectTypeDefinition? GetInputObjectType(IType? type)
        => type?.AsTypeDefinition() as IInputObjectTypeDefinition;

    private static CompositeResultElement ResolvePath(
        ISchemaDefinition schema,
        CompositeResultElement result,
        PathNode path)
    {
        if (result.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        if (path.TypeName is not null)
        {
            var type = schema.Types.GetType<IOutputTypeDefinition>(path.TypeName.Value);

            if (!type.IsAssignableFrom(result.AssertSelectionSet().Type))
            {
                return default;
            }
        }

        var currentSegment = path.PathSegment;
        var currentResult = result;
        var currentValueKind = result.ValueKind;

        while (currentSegment is not null && currentValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            if (!currentResult.TryGetProperty(currentSegment.FieldName.Value, out var fieldResult))
            {
                return default;
            }

            var fieldResultValueKind = fieldResult.ValueKind;

            if (fieldResultValueKind is JsonValueKind.Null)
            {
                return fieldResult;
            }

            if (currentSegment.TypeName is not null)
            {
                if (fieldResultValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidSelectionMapPathException(path);
                }

                currentResult = fieldResult;
                currentValueKind = fieldResultValueKind;

                var type = schema.Types.GetType<IOutputTypeDefinition>(currentSegment.TypeName.Value);

                if (!type.IsAssignableFrom(currentResult.AssertSelectionSet().Type))
                {
                    return default;
                }

                currentSegment = currentSegment.PathSegment;
                continue;
            }

            if (currentSegment.PathSegment is not null)
            {
                if (fieldResultValueKind is not JsonValueKind.Object)
                {
                    throw new InvalidSelectionMapPathException(path);
                }

                currentResult = fieldResult;
                currentSegment = currentSegment.PathSegment;
                continue;
            }

            return fieldResult;
        }

        return currentResult;
    }
}
