using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace StrawberryShake.Razor
{
    public abstract class QueryBase<TResult>
        : ComponentBase
        , IDisposable
        where TResult : class
    {
        private IDisposable? _subscription;
        private bool _isLoading = true;
        private bool _isErrorResult;
        private bool _isSuccessResult;
        private TResult? _result;
        private IReadOnlyList<IClientError>? _errors;
        private bool _disposed;

        [Parameter]
        public RenderFragment<TResult> Content { get; set; } = default!;

        [Parameter]
        public RenderFragment<IReadOnlyList<IClientError>>? Error { get; set; }

        [Parameter]
        public RenderFragment? Loading { get; set; }

        [Parameter]
        public ExecutionStrategy? Strategy { get; set; }

        protected void Subscribe(IObservable<IOperationResult<TResult>> observable)
        {
            _subscription?.Dispose();

            _subscription = observable.Subscribe(operationResult =>
            {
                _result = operationResult.Data;
                _errors = operationResult.Errors;
                _isErrorResult = operationResult.IsErrorResult();
                _isSuccessResult = operationResult.IsSuccessResult();
                _isLoading = false;
                StateHasChanged();
            });
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_isLoading && Loading is not null)
            {
                builder.AddContent(0, Loading);
            }

            if (_isErrorResult)
            {
                builder.AddContent(0, Error, _errors!);
            }

            if (_isSuccessResult)
            {
                builder.AddContent(0, Content, _result!);
            }

            base.BuildRenderTree(builder);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
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
}
