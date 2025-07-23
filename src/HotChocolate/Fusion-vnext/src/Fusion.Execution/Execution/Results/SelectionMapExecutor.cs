using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

internal sealed class FieldSelectionMapExecutor
{
    public IValueNode? Visit(ChoiceValueSelectionNode node, FieldSelectionMapExecutorContext context)
    {
        foreach (var entry in node.Entries)
        {
            var value = Visit(entry, context);

            if (value is null)
            {
                continue;
            }

            return value;
        }

        return null;
    }

    public IValueNode? Visit(SelectedValueEntryNode node, FieldSelectionMapExecutorContext context)
    {

    }



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

internal sealed class FieldSelectionMapExecutorContext
{
    public FieldSelectionMapExecutorContext(ISchemaDefinition schema)
    {
        Schema = schema;
    }

    public ISchemaDefinition Schema { get; }

    public Stack<IType> Types { get; } = new();

    public Stack<IType> InputTypes { get; } = new();

    public Stack<List<ResultData>> Results { get; } = new();

    public List<IValueNode?> Inputs { get; } = new();
}

internal sealed class InvalidSelectionMapPathException(PathNode path)
    : Exception($"The path is invalid: {path}");
