namespace HotChocolate.Pagination;

/// <summary>
/// This interceptor allows to capture paging queries for analysis.
/// </summary>
public abstract class PagingQueryInterceptor : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagingQueryInterceptor"/> class.
    /// </summary>
    protected PagingQueryInterceptor()
    {
        PagingQueryableExtensions.SetQueryInterceptor(this);
    }

    /// <summary>
    /// This method is called before the query is executed and allows to intercept it.
    /// </summary>
    /// <param name="query">
    /// The query that is about to be executed.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the query.
    /// </typeparam>
    public abstract void OnBeforeExecute<T>(IQueryable<T> query);

    /// <summary>
    /// The dispose call will remove the interceptor from the current scope.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            PagingQueryableExtensions.ClearQueryInterceptor(this);
            _disposed = true;
        }
    }
}
