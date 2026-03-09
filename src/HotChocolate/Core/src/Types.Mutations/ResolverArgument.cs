namespace HotChocolate.Types;

internal sealed class ResolverArgument(
    string name,
    string inputName,
    SchemaCoordinate coordinate,
    IInputType type,
    Type runtimeType,
    IValueNode? defaultValue,
    IInputValueFormatter? formatter)
    : IInputValueInfo
{
    public string Name { get; } = name;

    public string InputName { get; } = inputName;

    public SchemaCoordinate Coordinate { get; } = coordinate;

    public IInputType Type { get; } = type;

    public Type RuntimeType { get; } = runtimeType;

    public IValueNode? DefaultValue { get; } = defaultValue;

    public IInputValueFormatter? Formatter { get; } = formatter;
}
