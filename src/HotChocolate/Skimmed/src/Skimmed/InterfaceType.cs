namespace HotChocolate.Skimmed;

public sealed class InterfaceType : ComplexType
{
    public InterfaceType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Interface;
}
