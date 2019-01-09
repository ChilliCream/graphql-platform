using System;

namespace HotChocolate.Execution
{
    internal sealed class RequestServiceScope
        : IRequestServiceScope
    {
        private readonly IDisposable _scope;
        private bool _isLifetimeHandled;
        private bool _disposed;

        public RequestServiceScope(IServiceProvider services, IDisposable scope)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            ServiceProvider = services;
            _scope = scope;
        }

        public bool IsLifetimeHandled => _isLifetimeHandled;

        public IServiceProvider ServiceProvider { get; }

        public void HandleLifetime()
        {
            _isLifetimeHandled = true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _scope.Dispose();
                _disposed = true;
            }
        }
    }
}
