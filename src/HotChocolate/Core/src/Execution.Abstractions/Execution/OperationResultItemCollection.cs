using System.Collections;
using HotChocolate.Text.Json;

namespace HotChocolate.Execution;

/// <summary>
/// Represents the items of one batch of a streamed list and owns the memory that backs them.
/// </summary>
public abstract class OperationResultItemCollection
    : IEnumerable<OperationResultItem>
    , IRawJsonFormatter
    , IDisposable
{
    private int _disposed;

    /// <summary>
    /// Writes the items as a JSON array.
    /// </summary>
    /// <param name="jsonWriter">
    /// The JSON writer.
    /// </param>
    public abstract void WriteDataTo(JsonWriter jsonWriter);

    /// <summary>
    /// Returns an enumerator that yields the items of the batch in order.
    /// </summary>
    public abstract IEnumerator<OperationResultItem> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases the memory that backs the items. Subsequent calls have no effect.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Release();
        }
    }

    /// <summary>
    /// Releases the memory that backs the items. Called at most once.
    /// </summary>
    protected abstract void Release();
}
