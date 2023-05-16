namespace HotChocolate.Skimmed;

public sealed class InterfaceType : ComplexType, INamedTypeSystemMember<InterfaceType>
{
    public InterfaceType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Interface;

    public static InterfaceType Create(string name) => new(name);
}
