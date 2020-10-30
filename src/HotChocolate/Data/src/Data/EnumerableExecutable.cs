using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data
{
    public class EnumerableExecutable<T>
        : IExecutable<T>
        , IFirstOrDefaultExecutable
        , ISingleOrDefaultExecutable
        , IOffsetPagingExecutable
        , ICursorPagingExecutable
        , IQueryableFilteringExecutable<T>
        , IQueryableProjectionExecutable<T>
        , IQueryableSortingExecutable<T>
    {
        private bool _firstOrDefault;
        private bool _singleOrDefault;
        private Expression<Func<T, bool>>? _filterExpression;
        private Func<IQueryable<T>, IQueryable<T>>? _sortExpression;
        private Expression<Func<T, T>>? _projectionExpression;
        private OffsetPagingArguments? _offsetPagingArguments;
        private CursorPagingArguments? _cursorPagingArguments;
        private bool _offsetPagingIncludeTotalCount;

        public EnumerableExecutable(IEnumerable<T> source)
        {
            Source = source;
        }

        protected virtual IEnumerable<T> Source { get; }

        public bool IsInMemory() => Source is not IQueryable;

        public IExecutable AddSorting(Func<IQueryable<T>, IQueryable<T>>? sort)
        {
            _sortExpression = sort;
            return this;
        }

        public IExecutable AddFiltering(Expression<Func<T, bool>>? filter)
        {
            _filterExpression = filter;
            return this;
        }

        public IExecutable AddProjections(Expression<Func<T, T>>? projection)
        {
            _projectionExpression = projection;
            return this;
        }

        public IExecutable AddFirstOrDefault(bool mode = true)
        {
            _firstOrDefault = mode;
            return this;
        }

        public IExecutable AddSingleOrDefault(bool mode = true)
        {
            _singleOrDefault = mode;
            return this;
        }

        public IExecutable AddPaging(
            PagingOptions options,
            OffsetPagingArguments? arguments,
            bool includeTotalCount)
        {
            _offsetPagingArguments = arguments;
            _offsetPagingIncludeTotalCount = includeTotalCount;
            return this;
        }

        public IExecutable AddPaging(
            PagingOptions options,
            CursorPagingArguments? arguments)
        {
            _cursorPagingArguments = arguments;
            return this;
        }

        public virtual async ValueTask<object?> ExecuteAsync(CancellationToken cancellationToken)
        {
            IQueryable<T> result;
            if (Source is IQueryable<T> q)
            {
                result = q;
            }
            else
            {
                result = Source.AsQueryable();
            }

            if (_sortExpression is not null)
            {
                result = ApplySorting(result, _sortExpression);
            }

            if (_filterExpression is not null)
            {
                result = ApplyFiltering(result, _filterExpression);
            }

            if (_projectionExpression is not null)
            {
                result = ApplyProjections(result, _projectionExpression);
            }

            if (_firstOrDefault)
            {
                return await ApplyFirstOrDefaultAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (_singleOrDefault)
            {
                return await ApplySingleOrDefaultAsync(result, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (_offsetPagingArguments is not null)
            {
                return await ApplyOffsetPagingAsync(
                        result,
                        _offsetPagingArguments.Value,
                        _offsetPagingIncludeTotalCount,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (_cursorPagingArguments is not null)
            {
                return await ApplyCursorPagingAsync(
                        result,
                        _cursorPagingArguments.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return await ApplyToListAsync(result, cancellationToken)
                .ConfigureAwait(false);
        }

        public string Print()
        {
            return Source.ToString() ?? "";
        }

        protected virtual async Task<object?> ApplySingleOrDefaultAsync(
            IQueryable<T> result,
            CancellationToken cancellationToken)
        {
            return await SingleOrDefaultExecutor
                .ExecuteAsync<T>(null, result, cancellationToken)
                .ConfigureAwait(false);
        }

        protected virtual async Task<object?> ApplyFirstOrDefaultAsync(
            IQueryable<T> result,
            CancellationToken cancellationToken)
        {
            return await FirstOrDefaultExecutor
                .ExecuteAsync<T>(result, cancellationToken)
                .ConfigureAwait(false);
        }

        protected virtual IQueryable<T> ApplyProjections(
            IQueryable<T> result,
            Expression<Func<T, T>> projectionExpression)
        {
            result = result.Select(projectionExpression);
            return result;
        }

        protected virtual IQueryable<T> ApplyFiltering(
            IQueryable<T> result,
            Expression<Func<T, bool>> filterExpression)
        {
            result = result.Where(filterExpression);
            return result;
        }

        protected virtual IQueryable<T> ApplySorting(
            IQueryable<T> result,
            Func<IQueryable<T>, IQueryable<T>> sortExpression)
        {
            result = sortExpression(result);
            return result;
        }

        protected virtual ValueTask<object> ApplyToListAsync(
            IQueryable<T> result,
            CancellationToken cancellationToken)
        {
            return new ValueTask<object>(result.ToList());
        }

        protected virtual async Task<object> ApplyCursorPagingAsync(
            IQueryable<T> result,
            CursorPagingArguments cursorPagingArguments,
            CancellationToken cancellationToken)
        {
            var count = await Task.Run(result.Count, cancellationToken)
                .ConfigureAwait(false);

            IQueryable<T> edges =
                QueryableCursorPagingHandler<T>.SliceSource(
                    result,
                    cursorPagingArguments,
                    out var offset);

            IReadOnlyList<IndexEdge<T>> selectedEdges =
                await QueryableCursorPagingHandler<T>
                    .ExecuteAsync(edges, offset, cancellationToken)
                    .ConfigureAwait(false);

            return QueryableCursorPagingHandler<T>.CreateConnection(selectedEdges, count);
        }

        protected virtual async Task<object> ApplyOffsetPagingAsync(
            IQueryable<T> result,
            OffsetPagingArguments offsetPagingArguments,
            bool offsetPagingIncludeTotalCount,
            CancellationToken cancellationToken)
        {
            IQueryable<T> slicedSource =
                QueryableOffsetPagingHandler<T>.SliceSource(
                    result,
                    offsetPagingArguments,
                    out IQueryable<T> original);

            List<T> items =
                await QueryableOffsetPagingHandler<T>.ExecuteAsync(
                    slicedSource,
                    cancellationToken);

            return QueryableOffsetPagingHandler<T>.CreateCollectionSegment(
                offsetPagingArguments,
                offsetPagingIncludeTotalCount,
                items,
                original);
        }
    }
}
