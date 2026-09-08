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
    /// Gets the empty context, used at the operation's root and by any group
    /// none of whose members resolve a <c>sizedFields</c> size.
    /// </summary>
    public static readonly IReadOnlyList<SizedFieldContext> None = [];

    /// <summary>
    /// Resolves the <c>sizedFields</c> context <paramref name="members"/>'
    /// field call hands down to its own child field groups: one entry per
    /// member whose own <c>@listSize</c> metadata declares a non-empty
    /// <c>sizedFields</c> and resolves a size for this call's
    /// <paramref name="arguments"/>.
    /// </summary>
    public static IReadOnlyList<SizedFieldContext> Resolve(
        CostSchemaSnapshot snapshot,
        CollectedFieldGroupMember[] members,
        IReadOnlyList<ArgumentNode> arguments)
    {
        List<SizedFieldContext>? entries = null;

        foreach (var member in members)
        {
            var metadata = snapshot.GetListSizeMetadata(member.ParentType.Name, member.Field.Name);

            if (metadata is null)
            {
                continue;
            }

            var slicingArguments = SlicingArgumentValues.Build(metadata, member.Field, arguments);

            if (ListSizeResolver.TryResolveSizedFieldSize(
                    metadata,
                    slicingArguments,
                    variableValues: null,
                    snapshot.Options.DefaultListSize,
                    out var size))
            {
                (entries ??= []).Add(new SizedFieldContext(metadata.SizedFields, size));
            }
        }

        return (IReadOnlyList<SizedFieldContext>?)entries ?? None;
    }

    /// <summary>
    /// Gets the sizes <paramref name="context"/> inherits down to
    /// <paramref name="fieldName"/>: one size per entry whose
    /// <see cref="SizedFieldContext.SizedFields"/> names it.
    /// </summary>
    public static double[] InheritedSizesFor(IReadOnlyList<SizedFieldContext> context, string fieldName)
    {
        if (context.Count == 0)
        {
            return [];
        }

        List<double>? sizes = null;

        foreach (var entry in context)
        {
            if (entry.SizedFields.Contains(fieldName, StringComparer.Ordinal))
            {
                (sizes ??= []).Add(entry.Size);
            }
        }

        return sizes?.ToArray() ?? [];
    }
}
