using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL enum type
    /// </summary>
    public interface IEnumType<T> : IEnumType
    {
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
            [NotNullWhen(true)]out T runtimeValue);
    }
}
