using System.Collections.Concurrent;
using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// An internal helper to get connection flags from the current resolver context.
/// </summary>
public static class ConnectionFlagsHelper
{
    private static readonly ConcurrentDictionary<string, SelectionSetNode> s_parsedSelectionSets = new();

    /// <summary>
    /// Gets the connection flags from the current resolver context.
    /// </summary>
    /// <remarks>
    /// The cache lives as long as the compiled operation, so a selection set gated by
    /// <c>@skip</c>/<c>@include</c> must not be cached: its flags depend on the request.
    /// </remarks>
    public static ConnectionFlags GetConnectionFlags(IResolverContext context)
        => IsConditional(context)
            ? CreateConnectionFlags(context)
            : context.Selection.Features.GetOrSetSafe(CreateConnectionFlags, context);

    private static bool IsConditional(IResolverContext context)
        => context.Selection.Features.GetOrSetSafe(
            static context => new ConditionalSelectionSet(HasConditionalSelectionSet(context)),
            context).Value;

    private static bool HasConditionalSelectionSet(IResolverContext context)
    {
        var selection = context.Selection;

        if (selection.IsLeaf)
        {
            return false;
        }

        foreach (var typeContext in selection.DeclaringOperation.GetPossibleTypes(selection))
        {
            if (selection.DeclaringOperation.GetSelectionSet(selection, typeContext).IsConditional)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record ConditionalSelectionSet(bool Value);

    private static ConnectionFlags CreateConnectionFlags(IResolverContext context)
    {
        if (!context.Selection.Field.Flags.HasFlag(CoreFieldFlags.Connection))
        {
            return ConnectionFlags.None;
        }

        var connectionFlags = ConnectionFlags.None;

        if (context.IsSelected("edges"))
        {
            connectionFlags |= ConnectionFlags.Edges;
        }

        if (context.IsSelected("nodes"))
        {
            connectionFlags |= ConnectionFlags.Nodes;
        }

        if (context.IsSelected("totalCount"))
        {
            connectionFlags |= ConnectionFlags.TotalCount;
        }

        var options = PagingHelper.GetPagingOptions(context.Schema, context.Selection.Field);

        if (options.PageInfoFields.Count > 0
            || ((options.EnableRelativeCursors ?? PagingDefaults.EnableRelativeCursors)
            && options.RelativeCursorFields.Count > 0))
        {
            var startSelections = context.Select();
            var selectionContext = new IsSelectedContext(context.Schema, startSelections);

            if (options.PageInfoFields.Count > 0)
            {
                if (ArePatternsMatched(startSelections, selectionContext, options.PageInfoFields))
                {
                    connectionFlags |= ConnectionFlags.PageInfo;
                }
            }

            if ((options.EnableRelativeCursors ?? PagingDefaults.EnableRelativeCursors)
                && options.RelativeCursorFields.Count > 0)
            {
                if (ArePatternsMatched(startSelections, selectionContext, options.RelativeCursorFields))
                {
                    connectionFlags |= ConnectionFlags.RelativeCursor;
                }
            }
        }

        return connectionFlags;
    }

    private static bool ArePatternsMatched(
        ISelectionCollection startSelections,
        IsSelectedContext selectionContext,
        ImmutableHashSet<string> patterns)
    {
        foreach (var fieldPattern in patterns)
        {
            // we reset the state here so that each visitation starts fresh.
            selectionContext.AllSelected = true;
            selectionContext.Selections.Clear();
            selectionContext.Selections.Push(startSelections);

            // we parse the selection pattern, we in essence use
            // a SelectionSetNode as a selection pattern.
            var selectionPattern = ParsePattern(fieldPattern);

            // then we visit the selection and if one selection of the selection pattern
            // is not hit we break the loop and signal that the pattern was not hit.
            IsSelectedVisitor.Instance.Visit(selectionPattern, selectionContext);

            // if however all selections of the selection pattern are
            // hit we exit early and return true.
            if (selectionContext.AllSelected)
            {
                return true;
            }
        }

        return false;
    }

    private static SelectionSetNode ParsePattern(string selectionSet)
        => s_parsedSelectionSets.GetOrAdd(selectionSet, static s => ParseSelectionSet($"{{ {s} }}"));
}
