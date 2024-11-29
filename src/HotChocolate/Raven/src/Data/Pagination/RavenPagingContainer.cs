using System.Collections.Immutable;
using HotChocolate.Types.Pagination;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Session;

namespace HotChocolate.Data.Raven.Pagination;

internal sealed class RavenPagingContainer<TEntity>(IAsyncDocumentQuery<TEntity> query)
{
    private IAsyncDocumentQuery<TEntity> _query = query.NoTracking();
    private TaskHolder? _totalCount;

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        if (_totalCount is null)
        {
            Interlocked.CompareExchange(ref _totalCount,
                new TaskHolder(() => _query.CountAsync(cancellationToken)),
                null);
        }

        return _totalCount.Execute();
    }

    public async Task<ImmutableArray<Edge<TEntity>>> QueryAsync(
        int offset,
        CancellationToken cancellationToken)
    {
        TaskCompletionSource<int>? totalCountCompletionSource = null;

        try
        {
            // we cannot do concurrent requests on the IAsyncDocumentSession and we can also get
            // the total count from the stream directly. There are two execution strategies.
            // 1. If CountAsync was already needed before, we wait until the completion of the query
            //    and then continue with the stream.
            // 2. If CountAsync was not called already, we can just get the totalCount out of the
            //    stream over the statistics. We use a task completion source to make sure a
            //    concurrent call on count async waits, until we are done with the query
            if (_totalCount is null)
            {
                totalCountCompletionSource = new TaskCompletionSource<int>();

                // capture the source, so it is not in the outer scope
                var source = totalCountCompletionSource;
                var originalTotalCount =
                    Interlocked.CompareExchange(ref _totalCount, new TaskHolder(source.Task), null);

                // in case CountAsync was already executed we reset the completion source and await
                // the CountAsync call so that we do not have concurrent requests on the async
                // session
                if (originalTotalCount is not null)
                {
                    totalCountCompletionSource = null;
                    await originalTotalCount.Execute();
                }
            }

            // We only load the query stats when we not already have fetched them.
            IAsyncEnumerator<StreamResult<TEntity>> cursor;
            if (totalCountCompletionSource is not null)
            {
                cursor = await _query
                    .AsAsyncEnumerable(out var stats, cancellationToken)
                    .ConfigureAwait(false);

                totalCountCompletionSource.SetResult(stats.TotalResults);
            }
            else
            {
                cursor = await _query
                    .AsAsyncEnumerable(cancellationToken)
                    .ConfigureAwait(false);
            }

            // page through the response and create the array
            var list = ImmutableArray.CreateBuilder<Edge<TEntity>>();
            var index = offset;
            while (await cursor.MoveNextAsync().ConfigureAwait(false))
            {
                list.Add(IndexEdge<TEntity>.Create(cursor.Current.Document, index++));
            }

            return list.ToImmutable();
        }
        catch (OperationCanceledException)
        {
            totalCountCompletionSource?.SetCanceled(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            totalCountCompletionSource?.SetException(ex);
            throw;
        }
        finally
        {
            if (totalCountCompletionSource is { Task.IsCompleted: false, })
            {
                totalCountCompletionSource.SetCanceled(cancellationToken);
            }
        }
    }

    public async ValueTask<List<TEntity>> ToListAsync(CancellationToken cancellationToken)
    {
        return await _query.ToListAsync(cancellationToken);
    }

    public RavenPagingContainer<TEntity> Skip(int skip)
    {
        _query = _query.Skip(skip);
        return this;
    }

    public RavenPagingContainer<TEntity> Take(int take)
    {
        _query = _query.Take(take);
        return this;
    }

    private sealed class TaskHolder
    {
        private readonly object _lock = new();
        private Task<int>? _task;
        private readonly Func<Task<int>> _factory;

        public TaskHolder(Func<Task<int>> factory)
        {
            _factory = factory;
        }

        public TaskHolder(Task<int> task)
        {
            _task = task;
            _factory = null!;
        }

        public Task<int> Execute()
        {
            if (_task is null)
            {
                lock (_lock)
                {
                    _task ??= _factory();
                }
            }

            return _task;
        }
    }
}

file static class LocalExtensions
{
    public static Task<IAsyncEnumerator<StreamResult<T>>> AsAsyncEnumerable<T>(
        this IAsyncDocumentQuery<T> self,
        CancellationToken cancellationToken)
    {
        var ravenQueryProvider = (AsyncDocumentQuery<T>)self;

        return ravenQueryProvider.AsyncSession.Advanced.StreamAsync(self, cancellationToken);
    }

    public static Task<IAsyncEnumerator<StreamResult<T>>> AsAsyncEnumerable<T>(
        this IAsyncDocumentQuery<T> self,
        out StreamQueryStatistics statistics,
        CancellationToken cancellationToken)
    {
        var ravenQueryProvider = (AsyncDocumentQuery<T>)self;

        return ravenQueryProvider.AsyncSession.Advanced
            .StreamAsync(self, out statistics, cancellationToken);
    }
}
