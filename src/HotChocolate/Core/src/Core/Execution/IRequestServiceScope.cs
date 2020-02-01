using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution
{
    public interface IRequestServiceScope
        : IServiceScope
    {
        /// <summary>
        /// <c>true</c>, if the execution request is handling the
        /// lifetime of this scope; otherwise, <c>false</c> if the
        /// scope shall be handled by the executor.
        /// </summary>
        bool IsLifetimeHandled { get; }

        /// <summary>
        /// Signals that the lifetime is being handled by the request.
        /// </summary>
        void HandleLifetime();
    }
}
