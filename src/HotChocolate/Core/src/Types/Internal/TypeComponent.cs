namespace HotChocolate.Internal;

public readonly record struct TypeComponent(TypeComponentKind Kind, IExtendedType Type)
{
    public override string ToString() => Kind.ToString();

    public static implicit operator TypeComponent(
        (TypeComponentKind, IExtendedType) component)
        => new(component.Item1, component.Item2);
}
