using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public interface ITypeDescriptor : ICodeDescriptor
    {
        public NameString Name { get; }

        public TypeKind Kind { get; }
    }
}
