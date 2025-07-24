using HotChocolate.Buffers;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal sealed class FieldSelectionMapExecutor
{
    private PooledArrayWriter? _writer;

    public IValueNode? Visit(IValueSelectionNode node, FieldSelectionMapExecutorContext context)
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

    public IValueNode? Visit(ChoiceValueSelectionNode node, FieldSelectionMapExecutorContext context)
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

    public IValueNode? Visit(PathNode node, FieldSelectionMapExecutorContext context)
    {
        var result = ResolvePath(context.Schema, context.Result, node);

        if (result is null)
        {
            return null;
        }

        var (data, _) = result.Value;

        // Note: to capture data from the introspection
        // system we would need to also cover raw field results.
        if (data is LeafFieldResult field)
        {
            if (field.HasNullValue)
            {
                return NullValueNode.Default;
            }

            _writer ??= new PooledArrayWriter();
            var parser = new JsonValueParser(buffer: _writer);
            return parser.Parse(field.Value);
        }

        throw new InvalidSelectionMapPathException(node);
    }

    public IValueNode? Visit(ObjectValueSelectionNode node, FieldSelectionMapExecutorContext context)
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

    public IValueNode? Visit(PathObjectValueSelectionNode node, FieldSelectionMapExecutorContext context)
    {
        var resolved = ResolvePath(context.Schema, context.Result, node.Path);

        if (resolved is null)
        {
            return null;
        }

        if (resolved.Value.Result is not ObjectFieldResult obj)
        {
            throw new InvalidOperationException("Only object results are supported.");
        }

        return Visit(node.ObjectValueSelection, new(context.Schema, resolved.Value.Type, obj));
    }

    public IValueNode? Visit(ListValueSelectionNode node, FieldSelectionMapExecutorContext context)
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

                    var value = Visit(node.ElementSelection, new(context.Schema, listResult.ElementType, item));

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

                    var value = Visit(node.ElementSelection, new(context.Schema, listResult.ElementType, item));

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

    public IValueNode? Visit(PathListValueSelectionNode node, FieldSelectionMapExecutorContext context)
    {
        var result = ResolvePath(context.Schema, context.Result, node.Path);

        if (result is null)
        {
            return null;
        }

        if (result.Value.Result is ListFieldResult listField)
        {
            return listField.Value is null
                ? NullValueNode.Default
                : Visit(node.ListValueSelection, new(context.Schema, context.Type, listField.Value));
        }

        return null;
    }

    private static (ResultData Result, IType Type)? ResolvePath(
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

            return (fieldResult, fieldResult.Selection.Type);
        }

        if (currentResult is null)
        {
            return null;
        }

        return (currentResult, currentResult.SelectionSet.Type);
    }
}

internal readonly ref struct FieldSelectionMapExecutorContext(ISchemaDefinition schema, IType type, ResultData result)
{
    public ISchemaDefinition Schema { get; } = schema;

    public IType Type { get; } = type;

    public ResultData Result { get; } = result;
}

internal sealed class InvalidSelectionMapPathException(PathNode path)
    : Exception($"The path is invalid: {path}");
