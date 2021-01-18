namespace StrawberryShake.CodeGeneration
{
    public class ListTypeDescriptor: ITypeDescriptor
    {
        public ListTypeDescriptor(bool isNullable, ITypeDescriptor innerType)
        {
            IsNullable = isNullable;
            InnerType = innerType;
        }
        
        /// <summary>
        /// Describes whether or not it is a nullable type reference
        /// </summary>
        public bool IsNullable { get; }

        public ITypeDescriptor InnerType { get; }

        public bool IsScalarType => InnerType.IsScalarType;

        public bool IsEntityType => InnerType.IsEntityType;

        public TypeKind Kind => InnerType.Kind;

        public string Name => InnerType.Name;
    }
}
