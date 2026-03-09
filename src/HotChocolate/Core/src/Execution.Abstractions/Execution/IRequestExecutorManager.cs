namespace HotChocolate.Execution;

/// <summary>
/// The request executor manager allows to resolve and evict an <see cref="IRequestExecutor"/>.
/// </summary>
public interface IRequestExecutorManager : IRequestExecutorProvider
{
    /// <summary>
    /// Evict the request executor and schema with the given name.
    /// </summary>
    /// <param name="schemaName"></param>
    void EvictExecutor(string? schemaName = null);
}
