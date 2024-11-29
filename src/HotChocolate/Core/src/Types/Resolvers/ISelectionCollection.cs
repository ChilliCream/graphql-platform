#nullable enable
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Resolvers;

/// <summary>
/// Represents a collection of selections.
/// </summary>
public interface ISelectionCollection : IReadOnlyList<ISelection>
{
    /// <summary>
    /// Selects all child fields that match the given field name and
    /// returns a <see cref="ISelectionCollection"/> containing
    /// these selections.
    /// </summary>
    /// <param name="fieldName">
    /// The field name to select.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ISelectionCollection"/> containing
    /// the selections that match the given field name.
    /// </returns>
    ISelectionCollection Select(string fieldName);

    /// <summary>
    /// Selects all child fields that match the given field names and
    /// returns a <see cref="ISelectionCollection"/> containing
    /// these selections.
    /// </summary>
    /// <param name="fieldNames">
    /// The field names to select.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ISelectionCollection"/> containing
    /// the selections that match the given field name.
    /// </returns>
    ISelectionCollection Select(ReadOnlySpan<string> fieldNames);

    /// <summary>
    /// Selects all selections where the typeContext is assignable from the field`s declaring type.
    /// </summary>
    /// <param name="typeContext">
    /// The type context to select.
    /// </param>
    /// <returns>
    /// Returns a <see cref="ISelectionCollection"/> containing
    /// the selections where the typeContext is assignable from the field`s declaring type.
    /// </returns>
    ISelectionCollection Select(INamedType typeContext);

    /// <summary>
    /// Specifies if a child field with the given field name is selected.
    /// </summary>
    /// <param name="fieldName">
    /// The field name to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if a child field with the given field name is selected; otherwise, <c>false</c>.
    /// </returns>
    bool IsSelected(string fieldName);

    /// <summary>
    /// Specifies if a child field with one of the given field names is selected.
    /// </summary>
    /// <param name="fieldName1">
    /// The first field name to check.
    /// </param>
    /// <param name="fieldName2">
    /// The second field name to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if a child field with one of the given field names is selected; otherwise, <c>false</c>.
    /// </returns>
    bool IsSelected(string fieldName1, string fieldName2);

    /// <summary>
    /// Specifies if a child field with one of the given field names is selected.
    /// </summary>
    /// <param name="fieldName1">
    /// The first field name to check.
    /// </param>
    /// <param name="fieldName2">
    /// The second field name to check.
    /// </param>
    /// <param name="fieldName3">
    /// The third field name to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if a child field with one of the given field names is selected; otherwise, <c>false</c>.
    /// </returns>
    bool IsSelected(string fieldName1, string fieldName2, string fieldName3);

    /// <summary>
    /// Specifies if a child field with one of the given field names is selected.
    /// </summary>
    /// <param name="fieldNames">
    /// The field names to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if a child field with one of the given field names is selected; otherwise, <c>false</c>.
    /// </returns>
    bool IsSelected(ISet<string> fieldNames);
}
