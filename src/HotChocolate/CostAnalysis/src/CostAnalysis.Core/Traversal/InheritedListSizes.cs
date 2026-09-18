using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Resolves the <c>@listSize(sizedFields:)</c> context a field group hands
/// down to its own children, and looks up the inherited sizes a child field
/// group receives from it, one level at a time.
/// </summary>
internal static class InheritedListSizes
{
    /// <summary>
    /// Resolves the <c>sizedFields</c> context one field occurrence and
    /// parent-type pair hands down to its child selection boundary.
    /// </summary>
    public static SizedFieldContext? Resolve(
        CostSchemaIndex schemaIndex,
        CollectedFieldGroupMember member,
        IReadOnlyList<ArgumentNode> arguments)
    {
        var metadata = schemaIndex.GetListSizeMetadata(member.ParentType.Name, member.Field.Name);

        if (metadata is null)
        {
            return null;
        }

        var slicingArguments = SlicingArgumentValues.Build(metadata, member.Field, arguments);

        return metadata.SizedFields.Length > 0
            ? new SizedFieldContext(metadata, slicingArguments, schemaIndex.DefaultListSize)
            : null;
    }

    /// <summary>
    /// Gets the size <paramref name="context"/> inherits down to
    /// <paramref name="fieldName"/>, if its sized fields name that field.
    /// </summary>
    /// <param name="context">
    /// The parent's resolved <c>sizedFields</c> context.
    /// </param>
    /// <param name="fieldName">
    /// The child field to resolve an inherited size for.
    /// </param>
    /// <param name="variableValues">
    /// The coerced variable values to resolve a variable-bound slicing
    /// argument against, or <see langword="null"/> for the static/assumed
    /// path.
    /// </param>
    public static double? InheritedSizeFor(
        SizedFieldContext? context,
        string fieldName,
        ICostVariableValues? variableValues)
    {
        if (context is not { } entry
            || !entry.TryResolve(fieldName, variableValues, out var size))
        {
            return null;
        }

        return size;
    }
}
