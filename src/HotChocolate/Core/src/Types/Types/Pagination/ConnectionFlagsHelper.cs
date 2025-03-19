using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// An internal helper to get connection flags from the current resolver context.
/// </summary>
public static class ConnectionFlagsHelper
{
    private const string _keyFormat = "HotChocolate.Types.Pagination.ConnectionFlags_{0}";

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
                    var selectionContext = new IsSelectedContext(
                        ctx.Schema,
                        ctx.Select());

                    foreach (var relativeCursor in options.RelativeCursorFields)
                    {
                        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {relativeCursor} }}");
                        IsSelectedVisitor.Instance.Visit(selectionSet, selectionContext);
                        if (selectionContext.AllSelected)
                        {
                            connectionFlags |= ConnectionFlags.None;
                            break;
                        }
                    }
                }

                return connectionFlags;
            },
            context);
    }
}
