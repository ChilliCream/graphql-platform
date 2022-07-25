using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Language;
using static HotChocolate.Fusion.Execution.JsonValueToGraphQLValueConverter;

namespace HotChocolate.Fusion.Execution;

internal sealed class ExecutionState : IExecutionState
{
    private static readonly ListValueNode _emptyList = new(Array.Empty<IValueNode>());
    private readonly ConcurrentDictionary<string, State> _store = new();

    public void TryGetState(string key, ITypeNode expectedType, out IValueNode value)
        => throw new NotImplementedException();

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
        var literal = Convert(value);

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
