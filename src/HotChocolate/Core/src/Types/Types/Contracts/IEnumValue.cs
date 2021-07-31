using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL enum value.
    /// </summary>
    public interface IEnumValue
        : IHasDirectives
        , IHasReadOnlyContextData
    {
        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        EnumValueDefinitionNode? SyntaxNode { get; }

        /// <summary>
        /// The GraphQL name of this enum value.
        /// </summary>
        NameString Name { get; }

        /// <summary>
        /// Gets the GraphQL description for this enum value.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Defines if this enum value is deprecated.
        /// </summary>
        bool IsDeprecated { get; }

        /// <summary>
        /// Gets the deprecation reason for this enum value.
        /// </summary>
        string? DeprecationReason { get; }

        /// <summary>
        /// Gets the runtime value.
        /// </summary>
        object Value { get; }
    }
}
