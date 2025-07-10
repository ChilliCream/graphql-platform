using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Represents a read-only collection of enum values.
/// </summary>
public interface IReadOnlyEnumValueCollection : IReadOnlyList<IEnumValue>
{
    /// <summary>
    /// Gets the enum value with the specified name.
    /// </summary>
    IEnumValue this[string name] { get; }

    /// <summary>
    /// Tries to get the <paramref name="value"/> for
    /// the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <param name="value">
    /// The GraphQL enum value.
    /// </param>
    /// <returns>
    /// <c>true</c> if the <paramref name="name"/> represents a value of this enum type;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value);

    /// <summary>
    /// Determines whether the collection contains an enum value with the specified name.
    /// </summary>
    /// <param name="name">
    /// The GraphQL enum value name.
    /// </param>
    /// <returns>
    /// <c>true</c> if the collection contains an enum value with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string name);
}
