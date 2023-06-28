using System.Security.Cryptography;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class ObjectType : ComplexType, INamedTypeSystemMember<ObjectType>
{
    public ObjectType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Object;

    public override string ToString()
        => RewriteObjectType(this).ToString(true);

    public static ObjectType Create(string name) => new(name);
}
