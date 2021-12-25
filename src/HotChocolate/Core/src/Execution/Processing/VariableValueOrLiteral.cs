using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal readonly struct VariableValueOrLiteral
{
    public VariableValueOrLiteral(IInputType type, object? value, IValueNode valueLiteral)
    {
        if (value is null && valueLiteral.Kind != SyntaxKind.NullValue)
        {
            // TODO : resource
            throw new ArgumentException(
                "The runtime value can only be null if the literal is also null.");
        }

        Type = type ?? throw new ArgumentNullException(nameof(type));
        Value = value;
        ValueLiteral = valueLiteral;
    }

    public IInputType Type { get; }

    public object? Value { get; }

    public IValueNode ValueLiteral { get; }
}
