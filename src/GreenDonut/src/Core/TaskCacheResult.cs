namespace GreenDonut;

/// <summary>
/// Holds the result of a cached task of <see cref="ITaskCache"/>
/// </summary>
public class TaskCacheResult
{
    public TaskCacheResult(TaskCacheKey key, object result)
    {
        Key = key;
        Result = result;
    }

    /// <summary>
    /// The <see cref="TaskCacheKey"/> of this result
    /// </summary>
    public TaskCacheKey Key { get; }

    /// <summary>
    /// The value returned by the task
    /// </summary>
    public object Result { get; }
}
