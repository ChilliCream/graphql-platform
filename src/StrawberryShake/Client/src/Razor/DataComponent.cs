using Microsoft.AspNetCore.Components;

namespace StrawberryShake.Razor;

/// <summary>
/// A data component can be used to remove boiler plate from using reactive data operations.
/// </summary>
/// <typeparam name="TClientOrOperation">
/// The client or operation this component shall interact with.
/// </typeparam>
public abstract class DataComponent<TClientOrOperation> : ComponentBase, IDisposable
{
    private readonly List<IDisposable> _subscriptions = [];
    private bool _disposed;

    /// <summary>
    /// Gets the client or operation.
    /// </summary>
    [Inject]
    protected internal TClientOrOperation ClientOrOperation { get; internal set; } = default!;

    /// <summary>
    /// Gets the data client.
    /// </summary>
    protected TClientOrOperation Client => ClientOrOperation;

    /// <summary>
    /// Gets the data operation.
    /// </summary>
    protected TClientOrOperation Operation => ClientOrOperation;

    /// <summary>
    /// Registers a data subscription with the component.
    /// The component will dispose any registered subscription when it is disposed.
    /// </summary>
    /// <param name="subscribe">
    /// The subscribe delegate creating a data subscription.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="subscribe"/> is <c>null</c>.
    /// </exception>
    public void Register(Func<TClientOrOperation, IDisposable> subscribe)
    {
        if (subscribe is null)
        {
            throw new ArgumentNullException(nameof(subscribe));
        }

        _subscriptions.Add(subscribe(ClientOrOperation));
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing,
    /// releasing, or resetting unmanaged resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
