#nullable enable

namespace HotChocolate.Types;

internal sealed class ResolverArgument : IInputFieldInfo
{
    public ResolverArgument(
        string name,
        FieldCoordinate coordinate,
        IInputType type,
        Type runtimeType,
        IValueNode? defaultValue,
        IInputValueFormatter? formatter)
    {
        Name = name;
        Coordinate = coordinate;
        RuntimeType = runtimeType;
        Type = type;
        DefaultValue = defaultValue;
        Formatter = formatter;
    }

    public string Name { get; }

    public FieldCoordinate Coordinate { get; }

    public IInputType Type { get; }

    public Type RuntimeType { get; }

    public IValueNode? DefaultValue { get; }

    public IInputValueFormatter? Formatter { get; }
}

