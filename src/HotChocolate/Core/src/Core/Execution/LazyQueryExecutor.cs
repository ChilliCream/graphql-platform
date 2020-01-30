using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal class LazyQueryExecutor
        : IQueryExecutor
    {
        private readonly object _sync = new object();
        private readonly Func<IQueryExecutor> _executorFactory;
        private IQueryExecutor _innerExecutor;
        private bool _disposed;

        public LazyQueryExecutor(Func<IQueryExecutor> executorFactory)
        {
            _executorFactory = executorFactory
                ?? throw new ArgumentNullException(nameof(executorFactory));
        }

        private IQueryExecutor InnerExecutor
        {
            get
            {
                if (_innerExecutor == null)
                {
                    lock (_sync)
                    {
                        if (_innerExecutor == null)
                        {
                            _innerExecutor = _executorFactory();
                        }
                    }
                }
                return _innerExecutor;
            }
        }

        public ISchema Schema => InnerExecutor.Schema;

        public Task<IExecutionResult> ExecuteAsync(
            IReadOnlyQueryRequest request,
            CancellationToken cancellationToken)
        {
            return InnerExecutor.ExecuteAsync(request, cancellationToken);
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
                    _innerExecutor?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
