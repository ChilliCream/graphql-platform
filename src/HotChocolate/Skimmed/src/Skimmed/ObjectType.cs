namespace HotChocolate.Skimmed;

public sealed class ObjectType : ComplexType, INamedTypeSystemMember<ObjectType>
{
    public ObjectType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Object;

    public static ObjectType Create(string name) => new(name);
}
