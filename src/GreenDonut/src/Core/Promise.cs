using System;
using System.Threading.Tasks;

namespace GreenDonut;

/// <summary>
/// Represents a promise to fetch data within the DataLoader.
/// A promise can be based on the actual value,
/// a <see cref="System.Threading.Tasks.Task{TResult}"/>,
/// or a <see cref="TaskCompletionSource{TResult}"/>.
/// </summary>
/// <typeparam name="TValue"></typeparam>
public readonly struct Promise<TValue> : IPromise
{
    private readonly TaskCompletionSource<TValue>? _completionSource;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Promise{TValue}"/> class
    /// </summary>
    /// <param name="value">
    /// The actual value of the promise.
    /// </param>
    public Promise(TValue value)
    {
        Task = System.Threading.Tasks.Task.FromResult(value);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Promise{TValue}"/> class
    /// </summary>
    /// <param name="task">
    /// The task that represents the promise.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="task"/> is <c>null</c>.
    /// </exception>
    public Promise(Task<TValue> task)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Promise{TValue}"/> class
    /// </summary>
    /// <param name="completionSource">
    /// The task completion source that represents the promise.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="completionSource"/> is <c>null</c>.
    /// </exception>
    public Promise(TaskCompletionSource<TValue> completionSource)
    {
        _completionSource = completionSource ?? throw new ArgumentNullException(nameof(completionSource));
        Task = completionSource.Task;
    }

    /// <summary>
    /// Gets the task that represents the promise.
    /// </summary>
    public Task<TValue> Task { get; }
    
    Task IPromise.Task => Task;

    void IPromise.TryCancel()
        => _completionSource?.TrySetCanceled();
    
    /// <summary>
    /// Implicitly converts a <see cref="TaskCompletionSource{TResult}"/> to a promise.
    /// </summary>
    /// <param name="promise">
    /// The <see cref="TaskCompletionSource{TResult}"/> to convert.
    /// </param>
    /// <returns>
    /// The promise that represents the task completion source.
    /// </returns>
    public static implicit operator Promise<TValue>(TaskCompletionSource<TValue> promise) 
        => new(promise);
}