using HotChocolate;

namespace StrawberryShake.CodeGeneration
{
    /// <summary>
    /// Describes a type reference like the type of a member, parameter or the return type of a method
    /// </summary>
    public class PropertyDescriptor
    {
        public PropertyDescriptor(NameString name, ITypeDescriptor type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// The name of the property
        /// </summary>
        public NameString Name { get; }

        /// <summary>
        /// The referenced type
        /// </summary>
        public ITypeDescriptor Type { get; }
    }
}
