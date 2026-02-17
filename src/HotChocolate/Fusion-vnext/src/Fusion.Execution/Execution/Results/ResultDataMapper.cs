using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Results;

internal static class ResultDataMapper
{
    public static IValueNode? Map(
        CompositeResultElement result,
        IValueSelectionNode valueSelection,
        ISchemaDefinition schema,
        ref PooledArrayWriter? writer)
    {
        var context = new Context(schema, result, ref writer);
        return Visit(valueSelection, context);
    }

    private static IValueNode? Visit(IValueSelectionNode node, Context context)
    {
        switch (node)
        {
            case ChoiceValueSelectionNode choice:
                return Visit(choice, context);

            case PathNode path:
                return Visit(path, context);

            case ObjectValueSelectionNode objectValue:
                return Visit(objectValue, context);

            case PathObjectValueSelectionNode objectValue:
                return Visit(objectValue, context);

            case PathListValueSelectionNode listValue:
                return Visit(listValue, context);

            default:
                throw new NotSupportedException("Unknown value selection node type.");
        }
    }

    private static IValueNode? Visit(ChoiceValueSelectionNode node, Context context)
    {
        foreach (var branch in node.Branches)
        {
            var value = Visit(branch, context);

            if (value is null)
            {
                continue;
            }

            return value;
        }

        return null;
    }

    private static IValueNode? Visit(PathNode node, Context context)
    {
        var result = ResolvePath(context.Schema, context.Result, node);
        var resultValueKind = result.ValueKind;

        if (resultValueKind is JsonValueKind.Undefined)
        {
            return null;
        }

        if (resultValueKind is JsonValueKind.Null)
        {
            return NullValueNode.Default;
        }

        // Note: to capture data from the introspection
        // system we would need to also cover raw field results.
        if (result.Selection is { IsLeaf: true })
        {
            if (resultValueKind is JsonValueKind.Array)
            {
                var items = new List<IValueNode>();
                context.Writer ??= new PooledArrayWriter();
                var parser = new JsonValueParser(buffer: context.Writer);

                foreach (var item in result.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Null)
                    {
                        items.Add(NullValueNode.Default);
                        continue;
                    }

                    items.Add(parser.Parse(item.GetRawValue(includeQuotes: true)));
                }

                return new ListValueNode(items);
            }

            context.Writer ??= new PooledArrayWriter();
            var scalarParser = new JsonValueParser(buffer: context.Writer);
            return scalarParser.Parse(result.GetRawValue(includeQuotes: true));
        }

        throw new InvalidSelectionMapPathException(node);
    }

    private static IValueNode? Visit(ObjectValueSelectionNode node, Context context)
    {
        var result = context.Result;
        var resultValueKind = result.ValueKind;

        if (resultValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        var fields = new List<ObjectFieldNode>();

        foreach (var field in node.Fields)
        {
            var value = field.ValueSelection is null
                ? Visit(new PathNode(new PathSegmentNode(field.Name)), context)
                : Visit(field.ValueSelection, context);

            if (value is null)
            {
                return null;
            }

            fields.Add(new ObjectFieldNode(field.Name.Value, value));
        }

        fields.Capacity = fields.Count;
        return new ObjectValueNode(fields);
    }

    private static IValueNode? Visit(PathObjectValueSelectionNode node, Context context)
    {
        var result = ResolvePath(context.Schema, context.Result, node.Path);
        var resultValueKind = result.ValueKind;

        if (resultValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (resultValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        return Visit(node.ObjectValueSelection, context.WithResult(result));
    }

    private static IValueNode? Visit(ListValueSelectionNode node, Context context)
    {
        var result = context.Result;
        var resultValueKind = result.ValueKind;

        if (resultValueKind is not JsonValueKind.Array)
        {
            return null;
        }

        var items = new List<IValueNode>();

        foreach (var item in result.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Null)
            {
                items.Add(NullValueNode.Default);
                continue;
            }

            var value = Visit(node.ElementSelection, context.WithResult(item));

            if (value is null)
            {
                return null;
            }

            items.Add(value);
        }

        return new ListValueNode(items);
    }

    private static IValueNode? Visit(PathListValueSelectionNode node, Context context)
    {
        var result = ResolvePath(context.Schema, context.Result, node.Path);
        var resultValueKind = result.ValueKind;

        switch (resultValueKind)
        {
            case JsonValueKind.Undefined:
                return null;

            case JsonValueKind.Null:
                return NullValueNode.Default;

            case JsonValueKind.Array:
                return Visit(node.ListValueSelection, context.WithResult(result));

            default:
                return null;
        }
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

    private readonly ref struct Context
    {
        private readonly ref PooledArrayWriter? _writer;

        public Context(ISchemaDefinition schema, CompositeResultElement result, ref PooledArrayWriter? writer)
        {
            Schema = schema;
            Result = result;
            _writer = ref writer;
        }

        public ISchemaDefinition Schema { get; }

        public CompositeResultElement Result { get; }

        public ref PooledArrayWriter? Writer => ref _writer;

        public Context WithResult(CompositeResultElement result)
            => new(Schema, result, ref _writer);
    }
}
