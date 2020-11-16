using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    public interface IEnumValue
        : IHasDirectives
        , IHasReadOnlyContextData
    {
        EnumValueDefinitionNode? SyntaxNode { get; }

        /// <summary>
        /// The GraphQL name of this enum value.
        /// </summary>
        NameString Name { get; }

        string? Description { get; }

        bool IsDeprecated { get; }

        string? DeprecationReason { get; }

        /// <summary>
        /// Gets the runtime value.
        /// </summary>
        object Value { get; }
    }
}
