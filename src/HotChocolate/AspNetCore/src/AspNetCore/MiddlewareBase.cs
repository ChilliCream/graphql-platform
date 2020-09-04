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
            SchemaName = schemaName;

            executorResolver.RequestExecutorEvicted += EvictRequestExecutor;
        }

        /// <summary>
        /// Gets the name of the schema that this middleware serves up.
        /// </summary>
        protected NameString SchemaName { get; }

        /// <summary>
        /// Invokes the next middleware in line.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/>.
        /// </param>
        protected Task NextAsync(HttpContext context) => _next(context);

        /// <summary>
        /// Resolves the executor for the selected schema.
        /// </summary>
        /// <param name="cancellationToken">
        /// The request cancellation token.
        /// </param>
        /// <returns>
        /// Returns the resolved schema.
        /// </returns>
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
                            SchemaName, cancellationToken);
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
            if (!_disposed && args.Name.Equals(SchemaName))
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _executor = null;
                _semaphore.Dispose();
                _disposed = true;
            }
        }
    }
}
