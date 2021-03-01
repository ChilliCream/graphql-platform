using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type reference like the type of a member, parameter or the return type of a method
    /// </summary>
    public class PropertyDescriptor
    {
        public PropertyDescriptor(
            NameString name,
            ITypeDescriptor type,
            string? description)
        {
            Name = name;
            Type = type;
            Description = description;
        }

        /// <summary>
        /// The name of the property
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// The referenced type
        /// </summary>
        public ITypeDescriptor Type { get; }

        /// <summary>
        /// The description of the property
        /// </summary>
        public string? Description { get; }
    }
}
