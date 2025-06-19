using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

// we must make this thread-safe
internal sealed class FetchResultStore
{
    private readonly ISchemaDefinition _schema;
    private readonly Operation _operation = default!;
    private readonly ObjectResult _root = new();
    private readonly uint _includeFlags;

    public FetchResultStore(ISchemaDefinition schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        _schema = schema;
    }

    public void Save(
        SelectionPath sourcePath,
        SourceSchemaResult result)
    {
        if (result.Path.IsRoot)
        {
            var selectionSet = _operation.RootSelectionSet;

            if (!_root.IsInitialized)
            {
                _root.Initialize(selectionSet, _includeFlags);
            }









        }


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

        if (paths.Count == 0)
        {
            return [];
        }

        var variableValueSets = ImmutableArray.CreateBuilder<VariableValues>();

        foreach (var requirement in paths)
        {
            var (path, node, element) = requirement;

            if (element.ValueKind == JsonValueKind.Object)
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
        private IEnumerator? _enumerator;
        private int _index;

        public DefaultResultNavigator(ResultNode node)
        {
            var position = new Position(
                node,
                node.Results[0],
                node.Results.IndexOf(node.Results[0]),
                node.Results[0].Data,
                null,
                Path.Root);
            _stack.Push(position);
            (_, _, _, Value, _, Path, _index) = position;
        }

        private DefaultResultNavigator(List<Position> state)
        {
            _stack.AddRange(state);

            if (_stack.TryPeek(out var current))
            {
                (_, _, _, Value, _, Path, _index) = current;
            }
        }

        public string? Name { get; private set; }

        public Path Path { get; private set; } = Path.Root;

        public JsonElement Value { get; private set; }

        public bool IsNull => Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null;

        public bool MoveToFirstChild()
        {
            if (Value.ValueKind == JsonValueKind.Object)
            {
                var enumerator = Value.EnumerateObject();
                if (enumerator.MoveNext())
                {
                    _enumerator = enumerator;
                    Value = enumerator.Current.Value;
                    Name = enumerator.Current.Name;
                    Path = Path.Append(Name);
                    var parent = _stack.Peek();
                    _stack.Push(
                        parent with
                        {
                            Element = Value,
                            Enumerator = _enumerator,
                            Path = Path,
                            Index = -1
                        });
                    return true;
                }
            }

            if (Value.ValueKind == JsonValueKind.Array)
            {
                var enumerator = Value.EnumerateArray();
                if (enumerator.MoveNext())
                {
                    _enumerator = enumerator;
                    _index = 0;
                    Value = enumerator.Current;
                    Path = Path.Append(_index);
                    var parent = _stack.Peek();
                    _stack.Push(
                        parent with
                        {
                            Element = Value,
                            Enumerator = _enumerator,
                            Path = Path,
                            Index = _index
                        });
                    return true;
                }
            }

            return false;
        }

        public bool MoveToNext()
        {
            if (_enumerator is null)
            {
                return false;
            }

            if (_enumerator.MoveNext())
            {
                switch (_enumerator.Current)
                {
                    case JsonProperty property:
                        Value = property.Value;
                        Path = Path.Parent.Append(property.Name);
                        break;

                    case JsonElement element:
                        Value = element;
                        Path = Path.Parent.Append(_index);
                        _index++;
                        break;
                }

                return true;
            }

            var position = _stack.Peek();
            if (position.Node.Nodes.TryGetValue(position.Path, out var node))
            {

            }

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

    private record Position(
        ResultNode Node,
        ResultInfo Info,
        int InfoIndex,
        JsonElement Element,
        IEnumerator? Enumerator,
        Path Path,
        int Index = -1);
}


internal sealed class ValueCompletion
{
    private readonly ISchemaDefinition _schema;
    private readonly ErrorHandling _errorHandling;
    private readonly uint _includeFlags;


    public ValueCompletion(ISchemaDefinition schema, uint includeFlags, ErrorHandling errorHandling)
    {
        ArgumentNullException.ThrowIfNull(schema);
        _schema = schema;
        _includeFlags = includeFlags;
        _errorHandling = errorHandling;
    }

    private void BuildResult(
        SelectionSet selectionSet,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        ObjectResult objectResult)
    {
        // we need to validate the data and create a GraphQL error if its not an object.
        foreach (var selection in selectionSet.Selections)
        {
            if (!selection.IsIncluded(_includeFlags))
            {
                continue;
            }

            if (data.TryGetProperty(selection.ResponseName, out var child))
            {
                // null / list / object / scalar
            }
        }
    }

    private bool TryCompleteValue(
        Selection selection,
        IType type,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ObjectResult? result)
    {


    }

    private bool TryCompleteList(
        Selection selection,
        IType type,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ListResult? result)
    {
        // we need to validate the data and create a GraphQL error if its not an object.
        var elementType = type.ListType().ElementType;
        var isNullable = elementType.IsNullableType();

        if (elementType.IsListType())
        {
            return TryCompleteNestedList(selection, elementType, isNullable, sourceSchemaResult, data, out result);
        }

        if (elementType.IsLeafType())
        {
            return TryCompleteLeafList(selection, elementType, isNullable, sourceSchemaResult, data, out result);
        }

        return TryCompleteObjectList(selection, elementType, isNullable, sourceSchemaResult, data, out result);
    }

    private bool TryCompleteNestedList(
        Selection selection,
        IType elementType,
        bool isNullable,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ListResult? result)
    {
        var listResult = new NestedListResult();

        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (!isNullable && _errorHandling is ErrorHandling.Propagate)
                {
                    result = null;
                    return false;
                }

                listResult.Items.Add(null);
                continue;
            }

            if (TryCompleteList(selection, elementType, sourceSchemaResult, item, out var elementResult))
            {
                listResult.Items.Add(listResult);
            }
            else
            {
                if (!isNullable)
                {
                    result = null;
                    return false;
                }

                listResult.Items.Add(null);
            }
        }

        result = listResult;
        return true;
    }

    private bool TryCompleteLeafList(
        Selection selection,
        IType elementType,
        bool isNullable,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ListResult? result)
    {
        var listResult = new LeafListResult();

        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (!isNullable && _errorHandling is ErrorHandling.Propagate)
                {
                    result = null;
                    return false;
                }

                listResult.Items.Add(default);
                continue;
            }

            listResult.Items.Add(item);
        }

        result = listResult;
        return true;
    }

    private bool TryCompleteObjectList(
        Selection selection,
        IType elementType,
        bool isNullable,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ListResult? result)
    {
        var listResult = new ObjectListResult();

        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (!isNullable && _errorHandling is ErrorHandling.Propagate)
                {
                    result = null;
                    return false;
                }

                listResult.Items.Add(null);
                continue;
            }

            if (TryCompleteObjectValue(selection, elementType, isNullable, sourceSchemaResult, item, out var elementResult))
            {
                listResult.Items.Add(elementResult);
            }
            else
            {
                if (!isNullable)
                {
                    result = null;
                    return false;
                }

                listResult.Items.Add(null);
            }
        }

        result = listResult;
        return true;
    }

    private bool TryCompleteObjectValue(
        Selection selection,
        IType type,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ObjectResult? result)
    {
        var objectType = GetType(type, data);
        var operation = selection.DeclaringSelectionSet.DeclaringOperation;
        var selectionSet = operation.GetSelectionSet(selection, objectType);
        var objectResult = new ObjectResult();
        objectResult.Initialize(selectionSet, _includeFlags);
        BuildResult(selectionSet, sourceSchemaResult, data, objectResult);
        result = objectResult;
        return true;
    }

    private static bool TryCompleteLeafValue(
        Selection selection,
        IType leafType,
        bool isNullable,
        SourceSchemaResult sourceSchemaResult,
        JsonElement data,
        [NotNullWhen(true)] out ObjectResult? result)
    {


    }

    private IObjectTypeDefinition GetType(IType type, JsonElement data)
    {
        var namedType = type.NamedType();

        if (namedType is IObjectTypeDefinition objectType)
        {
            return objectType;
        }

        var typeName = data.GetProperty(IntrospectionFieldNames.TypeNameSpan).GetString()!;
        return _schema.Types.GetType<IObjectTypeDefinition>(typeName);
    }
}

public enum ErrorHandling
{
    Propagate = 0,
    Ignore = 1,
    Stop = 2
}
