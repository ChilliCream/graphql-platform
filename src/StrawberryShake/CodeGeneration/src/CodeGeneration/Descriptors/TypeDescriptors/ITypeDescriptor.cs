namespace StrawberryShake.CodeGeneration
{
    public interface ITypeDescriptor: ICodeDescriptor
    {
        public string Name { get; }

        public TypeKind Kind { get; }
        public bool IsNullable { get; }
        public bool IsScalarType { get; }
        public bool IsEntityType { get; }

    }
}
