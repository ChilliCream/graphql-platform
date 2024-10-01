namespace GreenDonut;

/// <summary>
/// An options object to configure the behavior for <c>DataLoader</c>.
/// </summary>
public sealed class DataLoaderOptions
{
    /// <summary>
    /// Gets or sets the maximum batch size per request. If set to
    /// <c>0</c>, the request will not be cut into smaller batches. The
    /// default value is set to <c>1024</c>.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1024;

    /// <summary>
    /// Gets or sets a cache instance to either share a cache instance
    /// across several DataLoader or to provide a custom cache
    /// implementation. In case no cache instance is provided, the
    /// DataLoader will use the default cache implementation.
    /// The default value is set to <c>null</c>.
    /// </summary>
    public IPromiseCache? Cache { get; set; }

    /// <summary>
    /// Gets the <see cref="IDataLoaderDiagnosticEvents"/> to intercept DataLoader events.
    /// </summary>
    public IDataLoaderDiagnosticEvents? DiagnosticEvents { get; set; }

    /// <summary>
    /// Creates a new options object that contains all the property values of this instance.
    /// </summary>
    /// <returns>
    /// The new options object that contains all the property values of this instance.
    /// </returns>
    public DataLoaderOptions Copy()
        => new()
        {
            MaxBatchSize = MaxBatchSize,
            Cache = Cache,
            DiagnosticEvents = DiagnosticEvents,
        };
}
