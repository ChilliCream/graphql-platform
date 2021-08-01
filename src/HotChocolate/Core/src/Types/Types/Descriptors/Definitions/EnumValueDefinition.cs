using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// Defines the properties of a GraphQL enum value.
    /// </summary>
    public class EnumValueDefinition
        : TypeDefinitionBase<EnumValueDefinitionNode>
        , ICanBeDeprecated
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumValueDefinition"/>.
        /// </summary>
        public EnumValueDefinition() { }

        /// <summary>
        /// Initializes a new instance of <see cref="EnumValueDefinition"/>.
        /// </summary>
        public EnumValueDefinition(
            NameString name,
            string? description = null,
            object? runtimeValue = null)
        {
            Name = name;
            Description = description;
            RuntimeValue = runtimeValue;
        }

        /// <summary>
        /// Gets the reason why this value was deprecated.
        /// </summary>
        public string? DeprecationReason { get; set; }

        /// <summary>
        /// Defines if this enum value is deprecated.
        /// </summary>
        public bool IsDeprecated => !string.IsNullOrEmpty(DeprecationReason);

        /// <summary>
        /// Gets the runtime value.
        /// </summary>
        public object? RuntimeValue { get; set; }

        /// <summary>
        /// Gets or sets the enum value member.
        /// </summary>
        public MemberInfo? Member { get; set; }
    }
}
