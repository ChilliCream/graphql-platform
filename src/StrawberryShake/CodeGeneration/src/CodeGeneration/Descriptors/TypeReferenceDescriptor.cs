namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type reference like the type of a member, parameter or the return type of a method
    /// </summary>
    public class TypeReferenceDescriptor
        : ICodeDescriptor
    {
        /// <summary>
        /// The referenced type
        /// </summary>
        public TypeDescriptor Type { get; }

        /// <summary>
        /// Describes whether or not it is a nullable type reference
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// Describes if or what type of list the type reference is
        /// </summary>
        public ListType ListType { get; }

        public string TypeName => Type.Name;
        public bool IsEntityType => Type.Kind == TypeKind.EntityType;

        public TypeReferenceDescriptor(
            TypeDescriptor type,
            bool isNullable,
            ListType listType)
        {
            Type = type;
            IsNullable = isNullable;
            ListType = listType;
        }
    }
}
