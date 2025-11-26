using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Properties.Resources;

namespace HotChocolate.Execution.Processing;

internal readonly struct VariableValueOrLiteral
{
    public VariableValueOrLiteral(IInputType type, object? value, IValueNode valueLiteral)
    {
        if (value is null && valueLiteral.Kind != SyntaxKind.NullValue)
        {
            throw new ArgumentException(VariableValueOrLiteral_NullNotAllowed);
        }

        Type = type ?? throw new ArgumentNullException(nameof(type));
        Value = value;
        ValueLiteral = valueLiteral;
    }

    public IInputType Type { get; }

    public object? Value { get; }

    public IValueNode ValueLiteral { get; }
}
