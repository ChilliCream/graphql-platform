namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type reference like the type of a member, parameter or the return type of a method
    /// </summary>
    public class TypeReferenceDescriptor
        : ICodeDescriptor
    {
        /// <summary>
        /// The name of the referenced type
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Describes whether or not it is a nullable type reference
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// Describes if or what type of list the type reference is
        /// </summary>
        public ListType ListType { get; }

        /// <summary>
        /// Is the referenced type a reference type or a value type?
        /// </summary>
        public bool IsReferenceType { get; }

        /// <summary>
        /// Is the type a known EntityType?
        /// </summary>
        public bool IsEntityType { get; }

        public TypeReferenceDescriptor(
            string name,
            bool isNullable,
            ListType listType,
            bool isReferenceType,
            bool isEntityType = false)
        {
            Name = name;
            IsNullable = isNullable;
            ListType = listType;
            IsReferenceType = isReferenceType;
            IsEntityType = isEntityType;
        }
    }
}
