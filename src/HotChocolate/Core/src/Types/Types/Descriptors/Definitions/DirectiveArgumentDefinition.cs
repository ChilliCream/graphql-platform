using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// This definition represents a directive argument.
    /// </summary>
    public class DirectiveArgumentDefinition : ArgumentDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
        /// </summary>
        public DirectiveArgumentDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="ArgumentDefinition"/>.
        /// </summary>
        public DirectiveArgumentDefinition(
            NameString name,
            string? description = null,
            ITypeReference? type = null,
            IValueNode? defaultValue = null,
            object? runtimeDefaultValue = null)
        {
            Name = name;
            Description = description;
            Type = type;
            DefaultValue = defaultValue;
            RuntimeDefaultValue = runtimeDefaultValue;
        }

        /// <summary>
        /// The property to which this argument binds to.
        /// </summary>
        public PropertyInfo? Property { get; set; }
    }
}
