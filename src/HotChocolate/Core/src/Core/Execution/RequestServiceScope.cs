using System;

namespace HotChocolate.Execution
{
    internal sealed class RequestServiceScope
        : IRequestServiceScope
    {
        private readonly IDisposable _scope;
        private bool _disposed;

        public RequestServiceScope(
            IServiceProvider services,
            IDisposable scope)
        {
            ServiceProvider = services ??
                throw new ArgumentNullException(nameof(services));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public bool IsLifetimeHandled { get; private set; }

        public IServiceProvider ServiceProvider { get; }

        public void HandleLifetime()
        {
            IsLifetimeHandled = true;
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
