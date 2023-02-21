namespace HotChocolate.Skimmed;

public sealed class ObjectType : ComplexType
{
    public ObjectType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Object;
}
