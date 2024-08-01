namespace HotChocolate.Types;

internal sealed class ResolverArgument(
    string name,
    SchemaCoordinate coordinate,
    IInputType type,
    Type runtimeType,
    IValueNode? defaultValue,
    IInputValueFormatter? formatter)
    : IInputFieldInfo
{
    public string Name { get; } = name;

    public SchemaCoordinate Coordinate { get; } = coordinate;

    public IInputType Type { get; } = type;

    public Type RuntimeType { get; } = runtimeType;

    public IValueNode? DefaultValue { get; } = defaultValue;

    public IInputValueFormatter? Formatter { get; } = formatter;
}
