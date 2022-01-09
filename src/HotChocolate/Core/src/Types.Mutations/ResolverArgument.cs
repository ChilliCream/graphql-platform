#nullable enable

namespace HotChocolate.Types;

internal sealed class ResolverArgument : IInputFieldInfo
{
    public ResolverArgument(
        NameString name,
        FieldCoordinate coordinate,
        IInputType type,
        Type runtimeType,
        IValueNode? defaultValue,
        IInputValueFormatter? formatter,
        bool isDeprecated,
        string deprecationReason)
    {
        Name = name;
        Coordinate = coordinate;
        RuntimeType = runtimeType;
        Type = type;
        DefaultValue = defaultValue;
        Formatter = formatter;
        IsDeprecated = isDeprecated;
        DeprecationReason = deprecationReason;
    }

    public NameString Name { get; }

    public FieldCoordinate Coordinate { get; }

    public IInputType Type { get; }

    public Type RuntimeType { get; }

    public IValueNode? DefaultValue { get; }

    public IInputValueFormatter? Formatter { get; }

    public bool IsDeprecated { get; }

    public string? DeprecationReason { get; }
}

