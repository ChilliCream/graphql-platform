namespace StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

public sealed class ListTypeDescriptor: ITypeDescriptor
{
    public ListTypeDescriptor(ITypeDescriptor innerType)
    {
        InnerType = innerType;
    }

    public ITypeDescriptor InnerType { get; }

    public TypeKind Kind => InnerType.Kind;

    public string Name => InnerType.Name;
}
