using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    public interface ITypeDescriptor : ICodeDescriptor
    {
        public TypeKind Kind { get; }
    }
}
