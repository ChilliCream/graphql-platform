using System;

namespace HotChocolate.Data.ExpressionNodes;

public readonly struct Variable
{
    public object? Value { get; }
    public Type Type { get; }

    public Variable(object? value, Type type)
    {
        Value = value;
        Type = type;
    }
}
