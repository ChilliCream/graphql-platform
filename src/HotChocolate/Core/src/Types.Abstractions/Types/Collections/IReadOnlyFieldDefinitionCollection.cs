using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Represents a read-only collection of field definitions.
/// </summary>
/// <typeparam name="TField">
/// The type of the field definition.
/// </typeparam>
public interface IReadOnlyFieldDefinitionCollection<TField> : IReadOnlyList<TField> where TField : IFieldDefinition
{
    /// <summary>
    /// Gets the field definition with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the field definition.
    /// </param>
    TField this[string name] { get; }

    /// <summary>
    /// Tries to get the field definition with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the field definition.
    /// </param>
    /// <param name="field">
    /// The field definition.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the field definition was found; otherwise, <c>false</c>.
    /// </returns>
    bool TryGetField(string name, [NotNullWhen(true)] out TField? field);

    /// <summary>
    /// Determines whether the collection contains a field definition with the specified name.
    /// </summary>
    /// <param name="name">
    /// The name of the field definition.
    /// </param>
    /// <returns>
    /// <c>true</c>, if the collection contains a field definition with the specified name;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool ContainsName(string name);
}
