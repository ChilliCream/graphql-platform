using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL enum type
    /// </summary>
    public interface IEnumType : ILeafType
    {
        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        new EnumTypeDefinitionNode? SyntaxNode { get; }

        /// <summary>
        /// Gets the possible enum values.
        /// </summary>
        IReadOnlyCollection<IEnumValue> Values { get; }

        /// <summary>
        /// Tries to get the <paramref name="runtimeValue"/> for
        /// the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">
        /// The GraphQL enum value name.
        /// </param>
        /// <param name="runtimeValue">
        /// The .NET runtime value.
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="name"/> represents a value of this enum type;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool TryGetRuntimeValue(
            NameString name,
            [NotNullWhen(true)]out object? runtimeValue);
    }
}
