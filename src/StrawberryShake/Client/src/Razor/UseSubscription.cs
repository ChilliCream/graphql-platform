using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace StrawberryShake.Razor;

public abstract class UseSubscription<TResult> : ComponentBase, IDisposable where TResult : class
{
    private IDisposable? _subscription;
    private bool _isInitializing = true;
    private bool _isErrorResult;
    private bool _isSuccessResult;
    private TResult? _result;
    private IReadOnlyList<IClientError>? _errors;
    private bool _disposed;

    [Parameter] public RenderFragment<TResult>? ChildContent { get; set; }

    [Parameter] public RenderFragment<IReadOnlyList<IClientError>>? ErrorContent { get; set; }

    [Parameter] public RenderFragment? LoadingContent { get; set; }

    protected void Subscribe(IObservable<IOperationResult<TResult>> observable)
    {
        _subscription?.Dispose();

        _subscription = observable
            .Subscribe(operationResult =>
            {
                _result = operationResult.Data;
                _errors = operationResult.Errors;
                _isErrorResult = operationResult.IsErrorResult();
                _isSuccessResult = operationResult.IsSuccessResult();
                _isInitializing = false;
                InvokeAsync(StateHasChanged);
            });
    }

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _subscription?.Dispose();
            }

            _disposed = true;
        }
    }
}
