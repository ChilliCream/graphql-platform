using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal readonly struct VariableValue
{
    public VariableValue(string name, IInputType type, object? runtimeValue, IValueNode valueLiteral)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(valueLiteral);

        Name = name;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        RuntimeValue = runtimeValue;
        ValueLiteral = valueLiteral;
    }

    public string Name { get; }

    public IInputType Type { get; }

    public object? RuntimeValue { get; }

    public IValueNode ValueLiteral { get; }
}
