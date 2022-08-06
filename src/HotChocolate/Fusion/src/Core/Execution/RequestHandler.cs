using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestHandler
{
    private readonly string _schemaName;
    private readonly DocumentNode _document;
    private readonly IReadOnlyList<RequiredState> _requires;
    private readonly IReadOnlyList<ProvidedState> _provides;
    private readonly ObjectValueNode? _variables;


    internal RequestHandler(
        string schemaName,
        DocumentNode document,
        IReadOnlyList<RequiredState> requires,
        IReadOnlyList<ProvidedState> provides)
    {
        _schemaName = schemaName;
        _document = document;
        _requires = requires;
    }

    public DocumentNode Document => _document;

    public IReadOnlyList<ISelection> Selections { get; }

    public Request CreateRequest(IExecutionState executionState)
    {
        var variables = _variables;

        if (_requires.Count > 0)
        {
            var fieldStart = 0;
            var fieldSize = _requires.Count;
            ObjectFieldNode[]? fields = null;

            if (variables?.Fields.Count > 0)
            {
                fieldSize += variables.Fields.Count;
                fieldStart = variables.Fields.Count;
                fields = new ObjectFieldNode[fieldSize];

                for (var i = 0; i < variables.Fields.Count; i++)
                {
                    var field = variables.Fields[i];
                    fields[i] = field;
                }
            }
        }

        fields ??= new ObjectFieldNode[fieldSize];

            foreach (var requirement in _requires)
            {
                var stateValue = executionState.GetState(requirement.Key, requirement.Type);
                fields[fieldStart++] = new ObjectFieldNode(requirement.Key, stateValue);
            }

            variables = new ObjectValueNode(fields);
        }

        return new Request(_schemaName, _document, variables, null);
    }

    public bool TryExtractState(ExecutionState executionState, JsonElement data)
    {
        foreach (var providedState in _provides)
        {
            if (!TryExtractState(executionState, data))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryExtractState(
        ExecutionState executionState,
        ProvidedState state,
        int index,
        JsonElement current)
    {
        while (true)
        {
            if (index < state.Path.Count)
            {
                if (current.TryGetProperty(state.Path[index++], out var value))
                {
                    current = value;
                }
                else
                {
                    return false;
                }
            }

            if (index == state.Path.Count)
            {
                if (current.TryGetProperty(state.Key, out var value))
                {
                    executionState.AddState(state.Key, value, state.Type);
                }
                else
                {
                    return false;
                }
            }

            if (current.ValueKind is JsonValueKind.Object)
            {
                continue;
            }

            if (current.ValueKind is JsonValueKind.Array)
            {
                foreach (var element in current.EnumerateArray())
                {
                    if (!TryExtractState(executionState, state, index, element))
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    }
}

internal static class JsonValueToGraphQLValueConverter
{
    public static IValueNode Convert(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var fields = new List<ObjectFieldNode>();

                foreach (var property in element.EnumerateObject())
                {
                    fields.Add(new ObjectFieldNode(property.Name, Convert(property.Value)));
                }

                return new ObjectValueNode(fields);

            case JsonValueKind.Array:
                var index = 0;
                var items = new IValueNode[element.GetArrayLength()];

                foreach (var item in element.EnumerateArray())
                {
                    items[index++] = Convert(item);
                }

                return new ListValueNode(items);

            case JsonValueKind.String:
                return new StringValueNode(element.GetString()!);

            case JsonValueKind.Number:
                return Utf8GraphQLParser.Syntax.ParseValueLiteral(element.GetRawText());

            case JsonValueKind.True:
                return BooleanValueNode.True;

            case JsonValueKind.False:
                return BooleanValueNode.False;

            case JsonValueKind.Null:
                return NullValueNode.Default;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

internal readonly struct RequiredState
{
    public RequiredState(string key, IReadOnlyList<string> path, ITypeNode type)
    {
        Key = key;
        Path = path;
        Type = type;
    }

    public string Key { get; }

    public IReadOnlyList<string> Path { get; }

    public ITypeNode Type { get; }
}

internal readonly struct ProvidedState
{
    public ProvidedState(string key, IReadOnlyList<string> path, ITypeNode type)
    {
        Key = key;
        Path = path;
        Type = type;
    }

    public string Key { get; }

    public IReadOnlyList<string> Path { get; }

    public ITypeNode Type { get; }
}
