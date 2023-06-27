using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class InterfaceType : ComplexType, INamedTypeSystemMember<InterfaceType>
{
    public InterfaceType(string name) : base(name)
    {
    }

    public override TypeKind Kind => TypeKind.Interface;

    public override string ToString()
        => RewriteInterfaceType(this).ToString(true);

    public static InterfaceType Create(string name) => new(name);
}
