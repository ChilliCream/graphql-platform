namespace HotChocolate.Internal;

#if NET6_0_OR_GREATER
public readonly record struct TypeComponent(TypeComponentKind Kind, IExtendedType Type)
{
    public override string ToString() => Kind.ToString();

    public static implicit operator TypeComponent(
        (TypeComponentKind, IExtendedType) component) 
        => new(component.Item1, component.Item2);
}
#else
public readonly struct TypeComponent(TypeComponentKind kind, IExtendedType type)
{
    public TypeComponentKind Kind { get; } = kind;

    public IExtendedType Type { get; } = type;

    public override bool Equals(object obj)
    {
        return obj is TypeComponent component &&
               Kind == component.Kind &&
               Type == component.Type;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Kind.GetHashCode() * 397 ^ Type.GetHashCode() * 397;
        }
    }

    public override string ToString() => Kind.ToString();

    public static implicit operator TypeComponent(
        (TypeComponentKind, IExtendedType) component) =>
        new TypeComponent(component.Item1, component.Item2);
}
#endif