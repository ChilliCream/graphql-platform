using System.Collections.Concurrent;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// An internal helper to get connection flags from the current resolver context.
/// </summary>
public static class ConnectionFlagsHelper
{
    private const string _keyFormat = "HotChocolate.Types.Pagination.ConnectionFlags_{0}";
    private static readonly ConcurrentDictionary<string, SelectionSetNode> _parsedSelectionSets = new();

    /// <summary>
    /// Gets the connection flags from the current resolver context.
    /// </summary>
    public static ConnectionFlags GetConnectionFlags(IResolverContext context)
    {
        return context.Operation.GetOrAddState(
            string.Format(_keyFormat, context.Selection.Id),
            static (_, ctx) =>
            {
                if(ctx.Selection.Field is ObjectField field
                    && !field.Flags.HasFlag(FieldFlags.Connection))
                {
                    return ConnectionFlags.None;
                }

                var options = PagingHelper.GetPagingOptions(ctx.Schema, ctx.Selection.Field);

                var connectionFlags = ConnectionFlags.None;

                if(ctx.IsSelected("edges"))
                {
                    connectionFlags |= ConnectionFlags.Edges;
                }

                if(ctx.IsSelected("nodes"))
                {
                    connectionFlags |= ConnectionFlags.Nodes;
                }

                if(ctx.IsSelected("totalCount"))
                {
                    connectionFlags |= ConnectionFlags.TotalCount;
                }

                if ((options.EnableRelativeCursors ?? PagingDefaults.EnableRelativeCursors)
                    && options.RelativeCursorFields.Count > 0)
                {
                    var startSelections = ctx.Select();
                    var selectionContext = new IsSelectedContext(ctx.Schema, startSelections);

                    foreach (var relativeCursor in options.RelativeCursorFields)
                    {
                        // we reset the state here so that each visitation starts fresh.
                        selectionContext.AllSelected = true;
                        selectionContext.Selections.Clear();
                        selectionContext.Selections.Push(startSelections);

                        // we parse the selection pattern, we in essence use
                        // a SelectionSetNode as a selection pattern.
                        var selectionPattern = ParsePattern(relativeCursor);

                        // then we visit the selection and if one selection of the selection pattern
                        // is not hit we break the loop and do not set the relative cursor flag.
                        IsSelectedVisitor.Instance.Visit(selectionPattern, selectionContext);

                        // if however all selections of the selection pattern are
                        // hit we set the relative cursor flag.
                        if (selectionContext.AllSelected)
                        {
                            connectionFlags |= ConnectionFlags.RelativeCursor;
                            break;
                        }
                    }
                }

                return connectionFlags;
            },
            context);
    }

    private static SelectionSetNode ParsePattern(string selectionSet)
        => _parsedSelectionSets.GetOrAdd(selectionSet, static s => ParseSelectionSet($"{{ {s} }}"));
}
