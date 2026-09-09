using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One parent member's <c>@listSize(sizedFields:)</c> metadata and slicing
/// arguments, resolved only when a compiled plan is evaluated.
/// </summary>
/// <param name="Metadata">
/// The parent's list-size metadata.
/// </param>
/// <param name="SlicingArguments">
/// The parent's slicing arguments.
/// </param>
/// <param name="DefaultListSize">
/// The snapshot's default list size.
/// </param>
internal readonly record struct SizedFieldContext(
    ListSizeMetadata Metadata,
    IReadOnlyDictionary<string, SlicingArgumentValue> SlicingArguments,
    double DefaultListSize)
{
    public bool DependsOnVariables
    {
        get
        {
            foreach (var argument in SlicingArguments.Values)
            {
                if (argument.SuppliedValue is VariableNode)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool TryResolve(
        string fieldName,
        ICostVariableValues? variableValues,
        out double size)
    {
        if (!Metadata.SizedFields.Contains(fieldName, StringComparer.Ordinal))
        {
            size = 0.0;
            return false;
        }

        return ListSizeResolver.TryResolveSizedFieldSize(
            Metadata,
            SlicingArguments,
            variableValues,
            DefaultListSize,
            out size);
    }
}

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
        CostSchemaSnapshot snapshot,
        CollectedFieldGroupMember member,
        IReadOnlyList<ArgumentNode> arguments)
    {
        var metadata = snapshot.GetListSizeMetadata(member.ParentType.Name, member.Field.Name);

        if (metadata is null)
        {
            return null;
        }

        var slicingArguments = SlicingArgumentValues.Build(metadata, member.Field, arguments);

        return metadata.SizedFields.Length > 0
            ? new SizedFieldContext(metadata, slicingArguments, snapshot.DefaultListSize)
            : null;
    }

    /// <summary>
    /// Gets the size <paramref name="context"/> inherits down to
    /// <paramref name="fieldName"/>, if its sized fields name that field.
    /// </summary>
    public static double? InheritedSizeFor(SizedFieldContext? context, string fieldName)
    {
        if (context is not { } entry
            || !entry.TryResolve(fieldName, variableValues: null, out var size))
        {
            return null;
        }

        return size;
    }
}
