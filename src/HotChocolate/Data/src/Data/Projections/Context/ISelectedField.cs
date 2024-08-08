using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections.Context;

/// <summary>
/// Represents a field that is selected in a query in the context of an operation.
/// <remarks>
/// The difference between <see cref="ISelectedField"/> and <see cref="ISelection"/> is that
/// <see cref="ISelectedField"/> is specific to the current request/operation and not cached. This
/// makes it possible to recursively iterate through the selected fields by calling
/// <see cref="ISelectedField.GetFields"/>.
/// <example >
/// <code>
/// Queue&lt;string> visitedFields = new();
/// VisitFields(context.GetSelectedField());
///
/// void VisitFields(ISelectedField field)
/// {
///    visitedFields.Enqueue(field.Field.Name);
///    list.Add(string.Join(".", visitedFields));
///    foreach (ISelectedField subField in field.GetFields())
///    {
///        VisitFields(subField);
///    }
///    visitedFields.Dequeue();
/// }
/// </code>
/// </example>
/// </remarks>
/// </summary>
public interface ISelectedField
{
    /// <summary>
    /// Gets the field selection for which a field resolver is
    /// being executed.
    /// </summary>
    ISelection Selection { get; }

    /// <summary>
    /// Gets the field on which the field resolver is being executed.
    /// </summary>
    IObjectField Field { get; }

    /// <summary>
    /// Gets the  type of the field.
    /// </summary>
    IType Type { get; }

    /// <summary>
    /// Is true if the type of the selected field is a abstract type.
    /// Equivalent to <c>Type.IsAbstractType()</c>.
    /// </summary>
    bool IsAbstractType { get; }

    /// <summary>
    /// Get the selected fields of the sub selection of this field
    /// <remarks>
    /// If the field is an abstract type, the parameter <paramref name="type"/> is required
    /// </remarks>
    /// </summary>
    /// <param name="type">
    /// The type context. In case of an abstract type, there are multiple selection sets.
    /// The <paramref name="type"/> specifies which fields are returned.
    /// <remarks>
    /// This parameter is required if <see cref="ISelectedField.Type"/> is an
    /// abstract type.
    /// </remarks>
    /// </param>
    /// <param name="allowInternals">
    /// Include also internal selections that shall not be included into the result set.
    /// </param>
    /// <returns>
    /// Returns the selected fields of the sub selection of this field
    /// </returns>
    IReadOnlyList<ISelectedField> GetFields(ObjectType? type = null, bool allowInternals = false);

    /// <summary>
    /// Checks if a field is selected
    /// <remarks>
    /// If the field is an abstract type, the parameter <paramref name="type"/> is required
    /// </remarks>
    /// </summary>
    /// <param name="fieldName">
    /// Specifies the name of the field that is looked for.
    /// </param>
    /// <param name="type">
    /// The type context. In case of an abstract type, there are multiple selection sets.
    /// The <paramref name="type"/> specifies which fields are returned.
    /// <remarks>
    /// This parameter is required if <see cref="ISelectedField.Type"/> is an
    /// abstract type.
    /// </remarks>
    /// </param>
    /// <param name="allowInternals">
    /// Include also internal selections that shall not be included into the result set.
    /// </param>
    /// <returns>
    /// Returns true if the field is selected, false if the field is not selected
    /// </returns>
    bool IsSelected(string fieldName, ObjectType? type = null, bool allowInternals = true);
}
