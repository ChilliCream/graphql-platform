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
        , IPagingExecutable
        , IQueryableFilteringExecutable<T>
        , IQueryableProjectionExecutable<T>
        , IQueryableSortingExecutable<T>
    {
        private bool _firstOrDefault;
        private bool _singleOrDefault;
        private ApplyPagingToResultAsync? _pagingHandler;
        private Expression<Func<T, bool>>? _filterExpression;
        private Func<IQueryable<T>, IQueryable<T>>? _sortExpression;
        private Expression<Func<T, T>>? _projectionExpression;

        public EnumerableExecutable(IEnumerable<T> source)
        {
            Source = source;
        }

        protected virtual IEnumerable<T> Source { get; }

        public bool IsInMemory() => Source is not IQueryable;

        public IExecutable ApplyPaging(ApplyPagingToResultAsync? handler)
        {
            _pagingHandler = handler;
            return this;
        }

        public IExecutable ApplySorting(Func<IQueryable<T>, IQueryable<T>>? sort)
        {
            _sortExpression = sort;
            return this;
        }

        public IExecutable ApplyFiltering(Expression<Func<T, bool>>? filter)
        {
            _filterExpression = filter;
            return this;
        }

        public IExecutable ApplyProjection(Expression<Func<T, T>>? projection)
        {
            _projectionExpression = projection;
            return this;
        }

        public IExecutable FirstOrDefault(bool mode = true)
        {
            _firstOrDefault = mode;
            return this;
        }

        public IExecutable SingleOrDefault(bool mode = true)
        {
            _singleOrDefault = mode;
            return this;
        }

        public async ValueTask<object?> ExecuteAsync(CancellationToken cancellationToken)
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
                result = _sortExpression(result);
            }

            if (_filterExpression is not null)
            {
                result = result.Where(_filterExpression);
            }

            if (_projectionExpression is not null)
            {
                result = result.Select(_projectionExpression);
            }

            if (_firstOrDefault)
            {
                return await FirstOrDefaultExecutor
                    .ExecuteAsync<T>(result, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (_singleOrDefault)
            {
                return await FirstOrDefaultExecutor
                    .ExecuteAsync<T>(result, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (_pagingHandler is not null)
            {
                return await _pagingHandler(result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToList();
        }

        public string Print()
        {
            return Source.ToString() ?? "";
        }
    }
}
