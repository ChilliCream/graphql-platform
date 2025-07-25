using HotChocolate.Buffers;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal static class ResultDataMapper
{
    public static IValueNode? Map(
        ObjectResult result,
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

        if (result is null)
        {
            return null;
        }

        // Note: to capture data from the introspection
        // system we would need to also cover raw field results.
        if (result is LeafFieldResult field)
        {
            if (field.HasNullValue)
            {
                return NullValueNode.Default;
            }

            context.Writer ??= new PooledArrayWriter();
            var parser = new JsonValueParser(buffer: context.Writer);
            return parser.Parse(field.Value);
        }

        throw new InvalidSelectionMapPathException(node);
    }

    private static IValueNode? Visit(ObjectValueSelectionNode node, Context context)
    {
        if (context.Result is not ObjectResult)
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

        if (result is null)
        {
            return null;
        }

        if (result is not ObjectFieldResult obj)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        return Visit(node.ObjectValueSelection, context.WithResult(obj));
    }

    private static IValueNode? Visit(ListValueSelectionNode node, Context context)
    {
        switch (context.Result)
        {
            case ObjectListResult listResult:
            {
                var items = new List<IValueNode>();

                foreach (var item in listResult.Items)
                {
                    if (item is null)
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

            case NestedListResult listResult:
            {
                var items = new List<IValueNode>();

                foreach (var item in listResult.Items)
                {
                    if (item is null)
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

            default:
                return null;
        }
    }

    private static IValueNode? Visit(PathListValueSelectionNode node, Context context)
    {
        var result = ResolvePath(context.Schema, context.Result, node.Path);

        if (result is null)
        {
            return null;
        }

        if (result is ListFieldResult listField)
        {
            return listField.Value is null
                ? NullValueNode.Default
                : Visit(node.ListValueSelection, context.WithResult(listField.Value));
        }

        return null;
    }

    private static ResultData? ResolvePath(
        ISchemaDefinition schema,
        ResultData result,
        PathNode path)
    {
        if (result is not ObjectResult obj)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        if (path.TypeName is not null)
        {
            var type = schema.Types.GetType<IOutputTypeDefinition>(path.TypeName.Value);

            if (!type.IsAssignableFrom(obj.SelectionSet.Type))
            {
                return null;
            }
        }

        var currentSegment = path.PathSegment;
        var currentResult = obj;

        while (currentSegment is not null && currentResult is not null)
        {
            if (!currentResult.TryGetValue(currentSegment.FieldName.Value, out var fieldResult))
            {
                return null;
            }

            if (fieldResult.HasNullValue)
            {
                return null;
            }

            if (currentSegment.TypeName is not null)
            {
                if (fieldResult is not ObjectFieldResult objectFieldResult)
                {
                    throw new InvalidSelectionMapPathException(path);
                }

                currentResult = objectFieldResult.Value;

                var type = schema.Types.GetType<IOutputTypeDefinition>(currentSegment.TypeName.Value);

                if (!type.IsAssignableFrom(objectFieldResult.Value!.SelectionSet.Type))
                {
                    return null;
                }

                currentSegment = currentSegment.PathSegment;
                continue;
            }

            if (currentSegment.PathSegment is not null)
            {
                if (fieldResult is not ObjectFieldResult objectFieldResult)
                {
                    throw new InvalidSelectionMapPathException(path);
                }

                currentResult = objectFieldResult.Value;
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

        public Context(ISchemaDefinition schema, ResultData result, ref PooledArrayWriter? writer)
        {
            Schema = schema;
            Result = result;
            _writer = ref writer;
        }

        public ISchemaDefinition Schema { get; }

        public ResultData Result { get; }

        public ref PooledArrayWriter? Writer => ref _writer;

        public Context WithResult(ResultData result)
            => new(Schema, result, ref _writer);
    }
}
