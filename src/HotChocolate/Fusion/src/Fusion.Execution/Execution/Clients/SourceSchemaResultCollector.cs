using System.Buffers;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Collects the results of a source schema request in the order they arrive.
/// </summary>
/// <param name="initialCapacity">The number of results expected when a request returns more than one.</param>
internal sealed class SourceSchemaResultCollector(int initialCapacity) : ISourceSchemaResultSink
{
    private SourceSchemaResult? _single;
    private SourceSchemaResult[]? _buffer;
    private int _count;
    private bool _hasErrors;

    /// <summary>
    /// Gets the first result, or <c>null</c> when no result was collected.
    /// </summary>
    public SourceSchemaResult? Single => _single;

    /// <summary>
    /// Gets the rented buffer that holds the first <see cref="Count"/> results,
    /// or <c>null</c> when fewer than two results were collected.
    /// </summary>
    public SourceSchemaResult[]? Buffer => _buffer;

    /// <summary>
    /// Gets the number of collected results.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Gets whether any collected result carries errors.
    /// </summary>
    public bool HasErrors => _hasErrors;

    /// <inheritdoc />
    public bool OnResult(int requestIndex, SourceSchemaResult result)
    {
        if (_count == 0)
        {
            _single = result;
            _count = 1;
        }
        else
        {
            if (_buffer is null)
            {
                _buffer = ArrayPool<SourceSchemaResult>.Shared.Rent(Math.Max(initialCapacity, 2));
                _buffer[0] = _single!;
            }
            else if (_count == _buffer.Length)
            {
                Grow();
            }

            _buffer[_count++] = result;
        }

        // Parsing errors here allows the result store to reuse the cached value
        // and avoids a second document lookup per result.
        if (result.Errors is not null)
        {
            _hasErrors = true;
        }

        return true;
    }

    /// <summary>
    /// Disposes every collected result and returns the rented buffer. Calling it again has no effect.
    /// </summary>
    public void DisposeResults()
    {
        if (_buffer is not null)
        {
            foreach (var result in _buffer.AsSpan(0, _count))
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                result?.Dispose();
            }

            _buffer.AsSpan(0, _count).Clear();
            ArrayPool<SourceSchemaResult>.Shared.Return(_buffer);
            _buffer = null;
        }
        else
        {
            _single?.Dispose();
        }

        _single = null;
        _count = 0;
    }

    private void Grow()
    {
        var buffer = _buffer!;
        var grown = ArrayPool<SourceSchemaResult>.Shared.Rent(buffer.Length * 2);
        buffer.AsSpan(0, _count).CopyTo(grown);
        buffer.AsSpan(0, _count).Clear();
        ArrayPool<SourceSchemaResult>.Shared.Return(buffer);
        _buffer = grown;
    }
}
