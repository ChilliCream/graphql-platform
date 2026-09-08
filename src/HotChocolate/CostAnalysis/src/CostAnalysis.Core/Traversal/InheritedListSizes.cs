using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One parent member's resolved <c>@listSize(sizedFields:)</c> size and the
/// child field names it applies to.
/// </summary>
/// <param name="SizedFields">
/// The parent's <c>sizedFields</c> argument's names.
/// </param>
/// <param name="Size">
/// The size resolved for the parent's field call.
/// </param>
internal readonly record struct SizedFieldContext(ImmutableArray<string> SizedFields, double Size);

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

        return ListSizeResolver.TryResolveSizedFieldSize(
            metadata,
            slicingArguments,
            variableValues: null,
            snapshot.Options.DefaultListSize,
            out var size)
            ? new SizedFieldContext(metadata.SizedFields, size)
            : null;
    }

    /// <summary>
    /// Gets the size <paramref name="context"/> inherits down to
    /// <paramref name="fieldName"/>, if its sized fields name that field.
    /// </summary>
    public static double? InheritedSizeFor(SizedFieldContext? context, string fieldName)
    {
        if (context is not { } entry || !entry.SizedFields.Contains(fieldName, StringComparer.Ordinal))
        {
            return null;
        }

        return entry.Size;
    }
}
