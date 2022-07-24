using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class ExecutionState : IExecutionState
{
    private static readonly ListValueNode _emptyList = new(Array.Empty<IValueNode>());
    private readonly ConcurrentDictionary<string, State> _store = new();

    public IValueNode GetState(string key, ITypeNode expectedType)
    {
        if (_store.TryGetValue(key, out var state))
        {
            var stateValue = state.Value;

            if (expectedType.Equals(state.Type, SyntaxComparison.Syntax))
            {
                if (stateValue is IValueNode value)
                {
                    return value;
                }

                if(stateValue is ImmutableList<IValueNode> list)
                {
                    return list[0];
                }

                throw new InvalidOperationException("Unexpected State Value");
            }

            if (expectedType.IsListType() &&
                expectedType.InnerType().Equals(state.Type,SyntaxComparison.Syntax))
            {
                if (stateValue is IValueNode value)
                {
                    return new ListValueNode(value);
                }

                if(stateValue is ImmutableList<IValueNode> list)
                {
                    return new ListValueNode(list);
                }

                throw new InvalidOperationException("Unexpected State Value");
            }
        }

        throw new ArgumentException("State Not Found");
    }

    public void AddState(string key, JsonElement value, ITypeNode type)
    {
        var literal = CreateLiteral(value);

        if (_store.ContainsKey(key))
        {
            _store.AddOrUpdate(
                key,
                static (_, _) => throw new InvalidOperationException("State is never removed"),
                static (_, s, newValue) =>
                {
                    if (s.Value is IValueNode currentValue)
                    {
                        var builder = ImmutableList.CreateBuilder<IValueNode>();
                        builder.Add(currentValue);
                        builder.Add(newValue);
                        s.Value = builder.ToImmutable();
                    }
                    else if (s.Value is ImmutableList<IValueNode> list)
                    {
                        s.Value = list.Add(newValue);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected State Value");
                    }
                    return s;
                },
                literal);
        }
        else
        {
            var state = new State(type) { Value = literal };
            _store.AddOrUpdate(
                key,
                static (_, s) => s,
                static (_, s, newState) =>
                {
                    if (s.Value is IValueNode currentValue)
                    {
                        var builder = ImmutableList.CreateBuilder<IValueNode>();
                        builder.Add(currentValue);
                        builder.Add((IValueNode)newState.Value!);
                        s.Value = builder.ToImmutable();
                    }
                    else if (s.Value is ImmutableList<IValueNode> list)
                    {
                        s.Value = list.Add((IValueNode)newState.Value!);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected State Value");
                    }
                    return s;
                },
                state);
        }
    }

    private IValueNode CreateLiteral(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = new List<ObjectFieldNode>();

                foreach (var property in value.EnumerateObject())
                {
                    properties.Add(new ObjectFieldNode(property.Name, CreateLiteral(property.Value)));
                }

                return new ObjectValueNode(properties);

            case JsonValueKind.Array:
                var length = value.GetArrayLength();

                if (length is 0)
                {
                    return _emptyList;
                }

                var items = new IValueNode[length];
                var index = 0;

                foreach (var element in value.EnumerateArray())
                {
                    items[index++] = CreateLiteral(element);
                }

                return new ListValueNode(items);

            case JsonValueKind.String:
                return new StringValueNode(value.GetString()!);

            case JsonValueKind.Number:
                return Utf8GraphQLParser.Syntax.ParseValueLiteral(value.GetRawText());

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

    private sealed class State
    {
        public State(ITypeNode type)
        {
            Type = type;
        }

        public ITypeNode Type { get; }

        public object? Value { get; set; }
    }
}
