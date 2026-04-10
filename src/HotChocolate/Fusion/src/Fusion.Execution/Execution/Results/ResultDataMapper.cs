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
    /// value could not be resolved (undefined/null for required paths).
    /// </summary>
    public static bool TryMap(
        CompositeResultElement result,
        IValueSelectionNode valueSelection,
        ISchemaDefinition schema,
        JsonWriter writer)
        => Visit(valueSelection, result, schema, writer);

    private static bool Visit(
        IValueSelectionNode node,
        CompositeResultElement result,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                return VisitChoice(choice, result, schema, writer);

            case PathNode path:
                return VisitPath(path, result, schema, writer);

            case ObjectValueSelectionNode objectValue:
                return VisitObject(objectValue, result, schema, writer);

            case PathObjectValueSelectionNode pathObject:
                return VisitPathObject(pathObject, result, schema, writer);

            case PathListValueSelectionNode pathList:
                return VisitPathList(pathList, result, schema, writer);

            default:
                throw new NotSupportedException("Unknown value selection node type.");
        }
    }

    private static bool VisitChoice(
        ChoiceValueSelectionNode node,
        CompositeResultElement result,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        foreach (var branch in node.Branches)
        {
            if (Visit(branch, result, schema, writer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool VisitPath(
        PathNode node,
        CompositeResultElement result,
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
            writer.WriteNullValue();
            return true;
        }

        if (valueKind is JsonValueKind.Array)
        {
            WriteLeafArray(resolved, writer);
            return true;
        }

        writer.WriteRawValue(resolved.GetRawValue(includeQuotes: true));
        return true;
    }

    private static void WriteLeafArray(
        CompositeResultElement array,
        JsonWriter writer)
    {
        writer.WriteStartArray();

        foreach (var item in array.EnumerateArray())
        {
            var itemKind = item.ValueKind;

            if (itemKind is JsonValueKind.Null)
            {
                writer.WriteNullValue();
            }
            else if (itemKind is JsonValueKind.Array)
            {
                WriteLeafArray(item, writer);
            }
            else
            {
                writer.WriteRawValue(item.GetRawValue(includeQuotes: true));
            }
        }

        writer.WriteEndArray();
    }

    private static bool VisitObject(
        ObjectValueSelectionNode node,
        CompositeResultElement result,
        ISchemaDefinition schema,
        JsonWriter writer)
    {
        if (result.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        writer.WriteStartObject();

        foreach (var field in node.Fields)
        {
            writer.WritePropertyName(field.Name.Value);

            bool written;

            if (field.ValueSelection is null)
            {
                var pathNode = new PathNode(new PathSegmentNode(field.Name));
                written = VisitPath(pathNode, result, schema, writer);
            }
            else
            {
                written = Visit(field.ValueSelection, result, schema, writer);
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

        return VisitObject(node.ObjectValueSelection, resolved, schema, writer);
    }

    private static bool VisitPathList(
        PathListValueSelectionNode node,
        CompositeResultElement result,
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
                writer.WriteNullValue();
                return true;

            case JsonValueKind.Array:
                return VisitList(node.ListValueSelection, resolved, schema, writer);

            default:
                return false;
        }
    }

    private static bool VisitList(
        ListValueSelectionNode node,
        CompositeResultElement result,
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
                writer.WriteNullValue();
                continue;
            }

            if (!Visit(node.ElementSelection, item, schema, writer))
            {
                return false;
            }
        }

        writer.WriteEndArray();
        return true;
    }

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
