#nullable enable

using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// Adds a middleware that checks if the specified fields are selected.
/// This middleware adds a local state called `isSelected`.
/// <code>
/// <![CDATA[
/// [UseSelected("address", "items")]
/// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
/// {
///     // resolver code ...
/// }
/// ]]>
/// </code>
/// </summary>
[AttributeUsage(
    AttributeTargets.Parameter,
    Inherited = true,
    AllowMultiple = true)]
public class IsSelectedAttribute : Attribute
{
    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldName">
    /// The field name that we check for.
    /// </param>
    public IsSelectedAttribute(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException(
                "The field name must not be null or empty.",
                nameof(fieldName));
        }

        fieldName = $"{{ {fieldName} }}";

        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(fieldName);

        if (selectionSet.Selections.Count == 1 &&
            selectionSet.Selections[0] is FieldNode { SelectionSet: null } field)
        {
            FieldNames = [field.Name.Value];
        }
        else
        {
            Fields = selectionSet;
            FieldNames = [];
        }
    }

    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldName1">
    /// The first field name we check for.
    /// </param>
    /// <param name="fieldName2">
    /// The second field name we check for.
    /// </param>
    public IsSelectedAttribute(string fieldName1, string fieldName2)
    {
        FieldNames = [fieldName1, fieldName2,];
    }

    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldName1">
    /// The first field name we check for.
    /// </param>
    /// <param name="fieldName2">
    /// The second field name we check for.
    /// </param>
    /// <param name="fieldName3">
    /// The third field name we check for.
    /// </param>
    public IsSelectedAttribute(string fieldName1, string fieldName2, string fieldName3)
    {
        FieldNames = [fieldName1, fieldName2, fieldName3,];
    }

    /// <summary>
    /// Adds a middleware that checks if the specified fields are selected.
    /// This middleware adds a local state called `isSelected`.
    /// <code>
    /// <![CDATA[
    /// [UseSelected("address", "items")]
    /// public async Task<Order> GetOrderAsync(int id, [LocalState] bool isSelected)
    /// {
    ///     // resolver code ...
    /// }
    /// ]]>
    /// </code>
    /// </summary>
    /// <param name="fieldNames">
    /// The field names we check for.
    /// </param>
    public IsSelectedAttribute(params string[] fieldNames)
    {
        FieldNames = fieldNames;
    }

    /// <summary>
    /// Gets the field names we check for.
    /// </summary>
    public string[] FieldNames { get; }

    /// <summary>
    /// Gets the selection set that we check for field selections.
    /// </summary>
    public SelectionSetNode? Fields { get; }
}
