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
        if (context.Results.Count == 0)
        {
            return null;
        }

        if (context.Results is [ObjectResult objectResult])
        {
            var result = ResolvePath(context.Schema, objectResult, node);

            if (result is null)
            {
                return null;
            }

            var (data, type) = result.Value;

            // Note: to capture data from the introspection
            // system we would need to also cover raw field results.
            if (type.IsLeafType() && data is LeafFieldResult field)
            {
                if (field.HasNullValue)
                {
                    return NullValueNode.Default;
                }

                _writer ??= new PooledArrayWriter();
                var parser = new JsonValueParser(buffer: _writer);
                return parser.Parse(field.Value);
            }
        }

        throw new InvalidSelectionMapPathException(node);
    }

    public IValueNode? Visit(ObjectValueSelectionNode node, FieldSelectionMapExecutorContext context)
    {
        return null;
    }

    public IValueNode? Visit(PathObjectValueSelectionNode node, FieldSelectionMapExecutorContext context)
    {
        return null;
    }

    public IValueNode? Visit(PathListValueSelectionNode node, FieldSelectionMapExecutorContext context)
    {
        return null;
    }

    /*
       protected override ISyntaxVisitorAction Enter(
           PathNode node,
           FieldSelectionMapExecutorContext context)
       {
           var current = context.Results.Peek();

           var next = new List<ResultData>();

           foreach (var result in current)
           {
               var currentResult = result;

               if (currentResult is ObjectFieldResult objectFieldResult)
               {
                   currentResult = objectFieldResult.Value;
               }

               if (currentResult is not ObjectResult objectResult)
               {
                   continue;
               }

               var resolved = ResolvePath(context.Schema, objectResult, node);

               if (resolved is not null)
               {
                   next.Add(resolved.Value.Result);
               }
           }

           current.Clear();
           current.AddRange(next);

           return DefaultAction;
       }
       */

    private (ResultData Result, IType Type)? ResolvePath(
        ISchemaDefinition schema,
        ObjectResult obj,
        PathNode path)
    {
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

internal ref struct FieldSelectionMapExecutorContext
{
    public FieldSelectionMapExecutorContext(ISchemaDefinition schema, IType type, List<ResultData> results)
    {
        Schema = schema;
        Type = type;
        Results = results;
    }

    public ISchemaDefinition Schema { get; }

    public IType Type { get; }

    public List<ResultData> Results { get; } = new();
}

internal sealed class InvalidSelectionMapPathException(PathNode path)
    : Exception($"The path is invalid: {path}");
