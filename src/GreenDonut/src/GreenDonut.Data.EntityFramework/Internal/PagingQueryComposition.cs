using System.Linq.Expressions;
using GreenDonut.Data.Cursors;

namespace GreenDonut.Data.Internal;

/// <summary>
/// The result of composing the shared paging slicing logic for a queryable.
/// </summary>
/// <param name="OriginalQuery">
/// The query before any cursor predicate, order inversion or skip was applied. Used to
/// compute the total count of the unsliced dataset.
/// </param>
/// <param name="SlicedQuery">
/// The query with the cursor predicate, order inversion and skip applied, but without the
/// final <c>Take</c> that limits the page size.
/// </param>
/// <param name="Keys">
/// The cursor keys extracted from the queryable's order expressions.
/// </param>
/// <param name="Cursor">
/// The parsed cursor, or <c>null</c> if neither <c>after</c> nor <c>before</c> was specified.
/// </param>
/// <param name="Direction">
/// The direction in which the page is sliced from the dataset.
/// </param>
/// <param name="RequestedPageSize">
/// The number of items requested through <c>first</c> or <c>last</c>.
/// </param>
/// <param name="Offset">
/// The signed relative offset extracted from the cursor, or zero if none was specified.
/// </param>
/// <param name="Selector">
/// The selector extracted from the queryable, to be re-applied after paging, or
/// <c>null</c> if no selector needs to be re-applied.
/// </param>
/// <param name="Arguments">
/// The paging arguments, with defaults applied.
/// </param>
/// <param name="IncludeTotalCount">
/// If set to <c>true</c> the total count must be fetched as part of the page execution.
/// </param>
/// <param name="TotalCount">
/// The total count carried over from a relative cursor, or <c>null</c> if none is known yet.
/// </param>
/// <typeparam name="T">
/// The type of the items in the queryable.
/// </typeparam>
internal sealed record PagingQueryComposition<T>(
    IQueryable<T> OriginalQuery,
    IQueryable<T> SlicedQuery,
    CursorKey[] Keys,
    Cursor? Cursor,
    PagingDirection Direction,
    int RequestedPageSize,
    int Offset,
    Expression<Func<T, T>>? Selector,
    PagingArguments Arguments,
    bool IncludeTotalCount,
    int? TotalCount);
