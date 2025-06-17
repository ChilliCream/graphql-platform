using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

// we must make this thread-safe
internal sealed class FetchResultStore
{
    private readonly ResultNode _root = new();
    private readonly ISchemaDefinition _schema;
    private ImmutableDictionary<string, IValueNode> _nodes = ImmutableDictionary<string, IValueNode>.Empty;

    public FetchResultStore(ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _schema = schema;
    }

    public void Save(
        Path path,
        SelectionPath start,
        SourceSchemaResult result)
    {
        if (path.IsRoot)
        {
            _root.Results.Add(
                new ResultInfo
                {
                    Start = start,
                    StartElement = result.Data,
                    Result = result
                });
            return;
        }

        var pathStack = ToStack(path);
        var nodes = new Queue<(ResultNode Node, ImmutableStack<Path> Path)>();
        nodes.Enqueue((_root, pathStack));

        while (nodes.TryDequeue(out var current))
        {
            foreach (var (startPath, data) in current.Node.Results)
            {
                var currentPath = current.Path;
                var startElement = GetStartElement(startPath, data);
                if (TryResolvePath(startElement, ref currentPath, out var element))
                {
                    if (!current.Node.Nodes.TryGetValue(path, out var child))
                    {
                        child = new ResultNode();
                        current.Node.Nodes[path] = child;
                    }

                    child.Results.Add(
                        new ResultInfo
                        {
                            Start = start,
                            StartElement = startElement,
                            Result = result
                        });
                    return;
                }
                else if (current.Node.Nodes.TryGetValue(currentPath.Peek(), out var child))
                {
                    // we have a child node, so we continue to resolve the path
                    nodes.Enqueue((child, currentPath));
                }
            }
        }

        throw new InvalidOperationException(
            $"The path '{path}' could not be resolved in the result store.");
    }

    private static ImmutableStack<Path> ToStack(Path path)
    {
        var stack = ImmutableStack<Path>.Empty;
        var current = path;

        while (!current.IsRoot)
        {
            stack = stack.Push(current);
            current = current.Parent;
        }

        return stack;
    }

    private static JsonElement GetStartElement(
        SelectionPath start,
        JsonElement data)
    {
        if (start.IsRoot)
        {
            return data;
        }

        var current = data;

        for (var i = start.Segments.Length - 1; i >= 0; i--)
        {
            var segment = start.Segments[i];
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(segment.Name, out current))
            {
                throw new InvalidOperationException(
                    $"The path segment '{segment.Name}' does not exist in the data.");
            }
        }

        return current;
    }

    private static bool TryResolvePath(
        JsonElement start,
        ref ImmutableStack<Path> path,
        [NotNullWhen(true)] out JsonElement? element)
    {
        var currentPath = path;
        var currentElement = start;

        while (!currentPath.IsEmpty)
        {
            var nextPath = currentPath.Pop(out var segment);

            if (segment is IndexerPathSegment indexerSegment)
            {
                if (currentElement.ValueKind != JsonValueKind.Array)
                {
                    path = currentPath;
                    element = null;
                    return false;
                }

                currentElement = currentElement[indexerSegment.Index];
            }
            else if (segment is NamePathSegment nameSegment)
            {
                if (currentElement.ValueKind != JsonValueKind.Object
                    || !currentElement.TryGetProperty(nameSegment.Name, out currentElement))
                {
                    path = currentPath;
                    element = null;
                    return false;
                }
            }
            else
            {
                throw new NotSupportedException($"The path segment '{segment}' is not supported.");
            }

            currentPath = nextPath;
        }

        element = currentElement;
        return true;
    }

    public ImmutableArray<VariableValues> CreateVariableValueSets(
        SelectionPath selectionSet,
        IReadOnlyList<ObjectFieldNode> requestVariables,
        ImmutableArray<OperationRequirement> requiredData)
    {
        var paths = Expand(selectionSet);

        if(paths.Count == 0)
        {
            return [];
        }

        var variableValueSets = ImmutableArray.CreateBuilder<VariableValues>();

        foreach (var requirement in paths)
        {
            var (path, node, element) = requirement;

            if(element.ValueKind == JsonValueKind.Object)
            {
                var variableValues = new List<ObjectFieldNode>(requestVariables);

                // build variables

                variableValueSets.Add(new VariableValues(path, new ObjectValueNode(variableValues)));
            }
        }

        return variableValueSets.ToImmutableArray();
    }

    private List<(Path, ResultNode, JsonElement)> Expand(SelectionPath startPath)
    {
        var cursors = new List<(Path, ResultNode, JsonElement)>();
        var next = new List<(Path, ResultNode, JsonElement)>();

        foreach (var result in _root.Results)
        {
            cursors.Add((Path.Root, _root, result.Data));
        }

        for (var segmentIndex = 0; segmentIndex < startPath.Segments.Length; segmentIndex++)
        {
            var segment = startPath.Segments[segmentIndex];
            next.Clear();

            if (segment.Kind is SelectionPathSegmentKind.InlineFragment)
            {
                var typeCondition = _schema.Types[segment.Name];

                foreach (var (path, node, element) in cursors)
                {
                    if (element.ValueKind == JsonValueKind.Object
                        && element.TryGetProperty(IntrospectionFieldNames.TypeNameSpan, out var typeName)
                        && _schema.Types.TryGetType<IObjectTypeDefinition>(typeName.GetString()!, out var actualType)
                        && typeCondition.IsAssignableFrom(actualType))
                    {
                        next.Add((path, node, element));
                    }
                }

                if (next.Count == 0)
                {
                    return [];
                }

                (cursors, next) = (next, cursors);
                continue;
            }

            foreach (var (path, node, element) in cursors)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    if (element.TryGetProperty(segment.Name, out var child))
                    {
                        if (child.ValueKind == JsonValueKind.Null)
                        {
                            continue;
                        }

                        if (child.ValueKind == JsonValueKind.Array)
                        {
                            UnrollArray(path.Append(segment.Name), node, child);
                        }
                        else
                        {
                            next.Add((path.Append(segment.Name), node, child));
                        }
                    }
                    else if (node.Nodes.TryGetValue(path, out var resultNode))
                    {
                        foreach (var result in resultNode.Results)
                        {
                            var startElement = GetStartElement(result.Start, result.Data);
                            if (startElement.TryGetProperty(segment.Name, out child))
                            {
                                if (child.ValueKind == JsonValueKind.Null)
                                {
                                    continue;
                                }

                                if (child.ValueKind == JsonValueKind.Array)
                                {
                                    UnrollArray(path.Append(segment.Name), node, child);
                                }
                                else
                                {
                                    next.Add((path.Append(segment.Name), node, child));
                                }
                            }
                        }
                    }
                }
            }

            if (next.Count == 0)
            {
                return [];
            }

            (cursors, next) = (next, cursors);
        }

        return cursors;

        void UnrollArray(Path path, ResultNode node, JsonElement element)
        {
            for (var i = 0; i < element.GetArrayLength(); i++)
            {
                var item = element[i];

                if (item.ValueKind == JsonValueKind.Array)
                {
                    UnrollArray(path.Append(i), node, item);
                }
                else
                {
                    next.Add((path.Append(i), node, item));
                }
            }
        }
    }

    private sealed class ResultNode
    {
        public required Path Path { get; init; }

        public List<ResultInfo> Results { get; } = [];

        public Dictionary<Path, ResultNode> Nodes { get; } = [];
    }

    private sealed class ResultInfo
    {
        public required SelectionPath Start { get; init; }
        public required SourceSchemaResult Result { get; init; }
        public required JsonElement StartElement { get; init; }
        public JsonElement Data => Result.Data;

        public void Deconstruct(out SelectionPath start, out JsonElement data)
        {
            start = Start;
            data = Data;
        }
    }

    private sealed class WorkItem
    {
        public required ImmutableArray<ResultNode> Nodes { get; init; }
        public required ImmutableStack<SelectionPath.Segment> Start { get; init; }
        public required ImmutableList<Path> Paths { get; init; }
    }

    private sealed class StartNode
    {
        public required Path Path { get; init; }

        public required ResultNode Node { get; init; }

        public required JsonElement Data { get; init; }
    }

    private sealed class DefaultResultNavigator : IFetchResultStoreNavigator
    {
        private readonly List<Position> _stack = [];
        private int _index;

        public DefaultResultNavigator(ResultNode node, ResultInfo info, JsonElement element, Path path)
        {
            var position = new Position(node, info, node.Results.IndexOf(info), element, path, -1);
            _stack.Push(position);
            (_, _, _, Value, Path, _index) = position;
        }

        private DefaultResultNavigator(List<Position> state)
        {
            _stack.AddRange(state);

            if (_stack.TryPeek(out var current))
            {
                (_, _, _, Value, Path, _index) = current;
            }
        }

        public Path Path { get; private set; } = Path.Root;

        public JsonElement Value { get; private set; }

        public bool IsNull => Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null;

        public bool MoveToFirstChild()
        {
            return false;
        }

        public bool MoveToNext()
        {
            return false;
        }

        public bool MoveToParent()
        {
            return false;
        }

        public bool MoveToProperty(string name)
        {
            if (Value.ValueKind == JsonValueKind.Object)
            {
                return false;
            }

            if (Value.TryGetProperty(name, out var child))
            {
                var position = _stack.Peek();
                position = position with { Element = child, Path = position.Path.Append(name), Index = -1 };
                _stack.Push(position);
                Value = child;
                Path = position.Path;
                _index = -1;
                return true;
            }
            else
            {
                _stack.Peek()

            }

            return true;
        }

        public IFetchResultStoreNavigator Clone()
        {
            return new DefaultResultNavigator(_stack);
        }
    }

    private record struct Position(
        ResultNode Node,
        ResultInfo Info,
        int InfoIndex,
        JsonElement Element,
        Path Path,
        int Index);
}

public interface IFetchResultStoreNavigator
{
    JsonElement Value { get; }

    bool IsNull { get; }

    bool MoveToProperty(string name);

    bool MoveToParent();

    bool MoveToFirstChild();

    bool MoveToNext();

    IFetchResultStoreNavigator Clone();
}
