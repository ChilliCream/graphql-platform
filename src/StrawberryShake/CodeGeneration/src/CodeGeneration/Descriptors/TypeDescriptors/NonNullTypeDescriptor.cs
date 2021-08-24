using HotChocolate;

namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors
{
    public class NonNullTypeDescriptor : ITypeDescriptor
    {
        public NonNullTypeDescriptor(ITypeDescriptor innerType)
        {
            InnerType = innerType;
        }

        public ITypeDescriptor InnerType { get; }

        public TypeKind Kind => InnerType.Kind;

        public NameString Name => InnerType.Name;
    }
}
