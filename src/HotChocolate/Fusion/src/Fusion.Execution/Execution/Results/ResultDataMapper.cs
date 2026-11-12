using System.Text.Json;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;
using JsonWriter = HotChocolate.Text.Json.JsonWriter;

namespace HotChocolate.Fusion.Execution.Results;

internal static class ResultDataMapper
{
    /// <summary>
    /// Maps a value selection from the composite result and writes it directly as JSON.
    /// Returns <c>true</c> if the value was written successfully, <c>false</c> if the
    /// value could not be resolved (undefined/null for required paths) or a null value
    /// landed in a non-null position of the declared requirement type. A context path
    /// that names no element of <paramref name="contextRoots"/> cannot be resolved.
    /// </summary>
    public static bool TryMap(
        CompositeResultElement result,
        IValueSelectionNode valueSelection,
        ITypeNode type,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
        => Visit(valueSelection, result, type, schema, contextRoots, internalAlias, writer);

    private static bool Visit(
        IValueSelectionNode node,
        CompositeResultElement result,
        ITypeNode? type,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                return VisitChoice(choice, result, type, schema, contextRoots, internalAlias, writer);

            case PathNode path:
                return VisitPath(path, result, type, schema, contextRoots, internalAlias, writer);

            case ObjectValueSelectionNode objectValue:
                return VisitObject(objectValue, result, schema, contextRoots, internalAlias, writer);

            case PathObjectValueSelectionNode pathObject:
                return VisitPathObject(pathObject, result, type, schema, contextRoots, internalAlias, writer);

            case PathListValueSelectionNode pathList:
                return VisitPathList(pathList, result, type, schema, contextRoots, internalAlias, writer);

            default:
                throw new NotSupportedException("Unknown value selection node type.");
        }
    }

    private static bool VisitChoice(
        ChoiceValueSelectionNode node,
        CompositeResultElement result,
        ITypeNode? type,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
    {
        foreach (var branch in node.Branches)
        {
            if (Visit(branch, result, type, schema, contextRoots, internalAlias, writer))
            {
                return true;
            }
        }

        return false;
    }

    private static bool VisitPath(
        PathNode node,
        CompositeResultElement result,
        ITypeNode? type,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
    {
        var resolved = ResolvePath(schema, contextRoots, result, node, internalAlias);
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
        ITypeNode? elementType,
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
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
    {
        if (result.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        writer.WriteStartObject();

        foreach (var field in node.Fields)
        {
            var fieldInternalAlias =
                internalAlias is not null && node.Fields.Length == 1
                    ? internalAlias
                    : null;

            writer.WritePropertyName(field.Name.Value);

            bool written;

            if (field.ValueSelection is null)
            {
                var pathNode = new PathNode(new PathSegmentNode(field.Name));
                written = VisitPath(pathNode, result, null, schema, contextRoots, fieldInternalAlias, writer);
            }
            else
            {
                written = Visit(field.ValueSelection, result, null, schema, contextRoots, fieldInternalAlias, writer);
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
        ITypeNode? type,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
    {
        var resolved = ResolvePath(schema, contextRoots, result, node.Path, internalAlias);
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

        if (valueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        return VisitObject(node.ObjectValueSelection, resolved, schema, contextRoots, internalAlias: null, writer: writer);
    }

    private static bool VisitPathList(
        PathListValueSelectionNode node,
        CompositeResultElement result,
        ITypeNode? type,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        string? internalAlias,
        JsonWriter writer)
    {
        var resolved = ResolvePath(schema, contextRoots, result, node.Path, internalAlias);
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
                return VisitList(node.ListValueSelection, resolved, GetElementType(type), schema, contextRoots, writer);

            default:
                return false;
        }
    }

    private static bool VisitList(
        ListValueSelectionNode node,
        CompositeResultElement result,
        ITypeNode? elementType,
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
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

            if (!Visit(node.ElementSelection, item, elementType, schema, contextRoots, internalAlias: null, writer: writer))
            {
                return false;
            }
        }

        writer.WriteEndArray();
        return true;
    }

    private static bool IsNonNullPosition(ITypeNode? type)
        => type?.Kind is SyntaxKind.NonNullType;

    private static ITypeNode? GetElementType(ITypeNode? type)
        => type?.IsListType() == true ? type.ElementType() : null;

    private static bool TryGetContextRoot(
        ReadOnlySpan<ContextRootElement> contextRoots,
        string name,
        out CompositeResultElement root)
    {
        foreach (var contextRoot in contextRoots)
        {
            if (contextRoot.Name.Equals(name, StringComparison.Ordinal))
            {
                root = contextRoot.Element;
                return true;
            }
        }

        root = default;
        return false;
    }

    private static CompositeResultElement ResolvePath(
        ISchemaDefinition schema,
        ReadOnlySpan<ContextRootElement> contextRoots,
        CompositeResultElement result,
        PathNode path,
        string? internalAlias)
    {
        if (path.ContextName is not null)
        {
            // A context path starts at the element that sets the context. That element is
            // not the entity the requirement is anchored at, so an internal alias of the
            // requirement does not apply to it.
            if (!TryGetContextRoot(contextRoots, path.ContextName.Value, out result))
            {
                return default;
            }

            internalAlias = null;
        }

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
        var currentFieldName = internalAlias ?? currentSegment.FieldName.Value;

        while (currentSegment is not null && currentValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            if (!currentResult.TryGetProperty(currentFieldName, out var fieldResult))
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
                if (currentSegment is not null)
                {
                    currentFieldName = currentSegment.FieldName.Value;
                }
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
                if (currentSegment is not null)
                {
                    currentFieldName = currentSegment.FieldName.Value;
                }
                continue;
            }

            return fieldResult;
        }

        return currentResult;
    }
}

/// <summary>
/// The element that sets a context name for a requirement.
/// </summary>
/// <param name="Name">
/// The name of the context.
/// </param>
/// <param name="Element">
/// The result element that sets the context.
/// </param>
internal readonly record struct ContextRootElement(string Name, CompositeResultElement Element);
