using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using RequestDelegate = Microsoft.AspNetCore.Http.RequestDelegate;

namespace HotChocolate.AspNetCore
{
    public class MiddlewareBase : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly RequestDelegate _next;
        private readonly IRequestExecutorResolver _executorResolver;
        private readonly NameString _schemaName;
        private IRequestExecutor? _executor;
        private bool _disposed;

        protected MiddlewareBase(
            RequestDelegate next,
            IRequestExecutorResolver executorResolver,
            NameString schemaName)
        {
            _next = next ??
                throw new ArgumentNullException(nameof(next));
            _executorResolver = executorResolver ??
                throw new ArgumentNullException(nameof(executorResolver));
            _schemaName = schemaName;
        }

        protected NameString SchemaName => _schemaName;

        protected Task NextAsync(HttpContext context) => _next(context);

        protected async ValueTask<IRequestExecutor> GetExecutorAsync(
            CancellationToken cancellationToken)
        {
            IRequestExecutor? executor = _executor;

            if (executor is null)
            {
                await _semaphore.WaitAsync(cancellationToken);

                try
                {
                    if (_executor is null)
                    {
                        executor = await _executorResolver.GetRequestExecutorAsync(
                            _schemaName, cancellationToken);
                        _executor = executor;
                    }
                    else
                    {
                        executor = _executor;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            return executor;
        }

        private void EvictRequestExecutor(object? sender, RequestExecutorEvictedEventArgs args)
        {
            if (!_disposed && args.Name.Equals(_schemaName))
            {
                _semaphore.Wait();
                try
                {
                    _executor = null;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _executor = null;
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
