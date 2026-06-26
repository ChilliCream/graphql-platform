using System.Buffers;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace StrawberryShake.Razor;

/// <summary>
/// A base class for generated Blazor query components that survive the prerender to
/// interactive boundary. During a server prerender the latest result is saved through
/// <see cref="PersistentComponentState"/>; on the interactive client the saved payload is
/// taken back and rehydrated through the store so the query is not executed a second time.
/// </summary>
/// <typeparam name="TResult">
/// The operation result type.
/// </typeparam>
public abstract class UsePersistentQuery<TResult> : ComponentBase, IDisposable where TResult : class
{
    private readonly TaskCompletionSource _firstResult =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private IDisposable? _subscription;
    private PersistingComponentStateSubscription _persistingSubscription;
    private bool _registeredPersisting;
    private string? _subscribedKey;
    private JsonElement? _persistedData;
    private bool _isInitializing = true;
    private bool _isErrorResult;
    private bool _isSuccessResult;
    private TResult? _result;
    private IReadOnlyList<IClientError>? _errors;
    private bool _disposed;

    [Inject]
    internal PersistentComponentState PersistentComponentState { get; set; } = default!;

    [Parameter] public RenderFragment<TResult>? ChildContent { get; set; }

    [Parameter] public RenderFragment<IReadOnlyList<IClientError>>? ErrorContent { get; set; }

    [Parameter] public RenderFragment? LoadingContent { get; set; }

    [Parameter] public EventCallback<IOperationResult<TResult>> OnOperationResult { get; set; }

    /// <summary>
    /// Gets a stable persistence key for the current operation arguments. The same arguments
    /// must produce the same key on the server prerender and on the interactive client.
    /// </summary>
    protected abstract string GetPersistenceKey();

    /// <summary>
    /// Creates the watch observable for the current operation arguments. When
    /// <paramref name="persistedState"/> is provided the store is seeded from it and the
    /// result is served from the cache without a network call.
    /// </summary>
    /// <param name="persistedState">
    /// The persisted transport "data" payload, or <c>null</c> to execute normally.
    /// </param>
    protected abstract IObservable<IOperationResult<TResult>> CreateWatch(
        ReadOnlyMemory<byte>? persistedState);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var key = GetPersistenceKey();
        _subscribedKey = key;

        if (PersistentComponentState.TryTakeFromJson<JsonElement>(key, out var persisted))
        {
            // Interactive render after a server prerender: rehydrate the persisted payload
            // and serve it from the store without a network call.
            Subscribe(CreateWatch(ToUtf8(persisted)));
            return;
        }

        // Cold render (server prerender or non-prerendered): register a callback that saves
        // the latest payload so the interactive client can rehydrate it, then execute.
        _persistingSubscription = PersistentComponentState.RegisterOnPersisting(PersistAsync);
        _registeredPersisting = true;

        Subscribe(CreateWatch(persistedState: null));

        // Await the first result so prerendered HTML contains data instead of the loading
        // state and the persisting callback has a payload to save.
        await _firstResult.Task;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Re-subscribe only when the operation arguments actually change. This supports
        // changing parameters at runtime while avoiding a re-subscribe on every re-render
        // (for example when a parent passes fresh render-fragment delegates).
        var key = GetPersistenceKey();

        if (_subscribedKey is not null
            && !string.Equals(_subscribedKey, key, StringComparison.Ordinal))
        {
            _subscribedKey = key;
            Subscribe(CreateWatch(persistedState: null));
        }
    }

    private Task PersistAsync()
    {
        if (_persistedData is { } data)
        {
            PersistentComponentState.PersistAsJson(GetPersistenceKey(), data);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes the component to the specified result observable, disposing any previous
    /// subscription.
    /// </summary>
    protected void Subscribe(IObservable<IOperationResult<TResult>> observable)
    {
        _subscription?.Dispose();
        _subscription = observable.Subscribe(new Observer(this));
    }

    private void OnNext(IOperationResult<TResult> operationResult)
    {
        _result = operationResult.Data;
        _errors = operationResult.Errors;
        _isErrorResult = operationResult.IsErrorResult();
        _isSuccessResult = operationResult.IsSuccessResult();
        _isInitializing = false;

        if (operationResult.ContextData.TryGetValue(WellKnownContextData.PersistedData, out var payload)
            && payload is JsonElement element)
        {
            _persistedData = element;
        }

        _firstResult.TrySetResult();
        InvokeAsync(StateHasChanged);
        OnOperationResult.InvokeAsync(operationResult);
    }

    private void OnError(Exception error)
    {
        _errors = new IClientError[] { new ClientError(error.Message, exception: error) };
        _isErrorResult = true;
        _isSuccessResult = false;
        _isInitializing = false;
        _firstResult.TrySetResult();
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_isInitializing && LoadingContent is not null)
        {
            builder.AddContent(0, LoadingContent);
        }

        if (_isErrorResult)
        {
            builder.AddContent(0, ErrorContent, _errors!);
        }

        if (_isSuccessResult)
        {
            builder.AddContent(0, ChildContent, _result!);
        }

        base.BuildRenderTree(builder);
    }

    private static ReadOnlyMemory<byte> ToUtf8(JsonElement element)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();

        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            element.WriteTo(writer);
        }

        return bufferWriter.WrittenMemory;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the subscriptions held by the component.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _subscription?.Dispose();

                if (_registeredPersisting)
                {
                    _persistingSubscription.Dispose();
                }
            }

            _disposed = true;
        }
    }

    private sealed class Observer : IObserver<IOperationResult<TResult>>
    {
        private readonly UsePersistentQuery<TResult> _component;

        public Observer(UsePersistentQuery<TResult> component)
        {
            _component = component;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error) => _component.OnError(error);

        public void OnNext(IOperationResult<TResult> value) => _component.OnNext(value);
    }
}
