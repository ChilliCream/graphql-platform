using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class EnumValueDefinition
        : TypeDefinitionBase<EnumValueDefinitionNode>
        , ICanBeDeprecated
    {
        public string? DeprecationReason { get; set; }

        public bool IsDeprecated => !string.IsNullOrEmpty(DeprecationReason);

        public object? Value { get; set; }

        // <summary>
        /// Gets or sets the enum value member.
        /// </summary>
        public MemberInfo? Member { get; set; }
    }
}
