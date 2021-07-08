using System.Reflection;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// The <see cref="InterfaceFieldDefinition"/> contains the settings
    /// to create a <see cref="InterfaceField"/>.
    /// </summary>
    public class InterfaceFieldDefinition : OutputFieldDefinitionBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
        /// </summary>
        public InterfaceFieldDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectTypeDefinition"/>.
        /// </summary>
        public InterfaceFieldDefinition(
            NameString name,
            string? description = null,
            ITypeReference? type = null)
        {
            Name = name;
            Description = description;
            Type = type;
        }

        /// <summary>
        /// Gets the interface member to which this field is bound to.
        /// </summary>
        public MemberInfo? Member { get; set; }
    }
}
