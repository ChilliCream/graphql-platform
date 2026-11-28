using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// A stream of results that yields <paramref name="first"/> at once and stays in the move to its
/// second result until <see cref="Yield"/> is called or, if it honors cancellation, until the
/// enumeration is canceled.
/// </summary>
internal sealed class ManualResultStream(OperationResult first)
    : IAsyncEnumerable<OperationResult>
    , IAsyncEnumerator<OperationResult>
{
    private readonly TaskCompletionSource<bool> _second = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _moves;

    /// <summary>
    /// Gets a value indicating whether the move to the second result ends when the enumeration is canceled.
    /// </summary>
    public bool HonorsCancellation { get; init; }

    /// <summary>
    /// Gets the action that runs when the move to the second result starts, or <c>null</c> for none.
    /// </summary>
    public Action? OnSecondMove { get; init; }

    /// <summary>
    /// Gets the number of <see cref="DisposeAsync"/> calls.
    /// </summary>
    public int DisposeCount { get; private set; }

    public OperationResult Current { get; private set; } = first;

    public IAsyncEnumerator<OperationResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        if (HonorsCancellation)
        {
            cancellationToken.Register(() => _second.TrySetCanceled(cancellationToken));
        }

        return this;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        switch (_moves++)
        {
            case 0:
                return new ValueTask<bool>(true);

            case 1:
                OnSecondMove?.Invoke();
                return new ValueTask<bool>(_second.Task);

            default:
                return new ValueTask<bool>(false);
        }
    }

    /// <summary>
    /// Completes the move to the second result with <paramref name="result"/>.
    /// </summary>
    public void Yield(OperationResult result)
    {
        Current = result;
        _second.TrySetResult(true);
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return default;
    }
}
