using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Used for notifications when <see cref="RequestExecutorFactoryOptions"/> instances change.
    /// </summary>
    public interface IRequestExecutorOptionsMonitor
    {
        /// <summary>
        /// Returns a configured <see cref="RequestExecutorFactoryOptions"/>
        /// instance with the given name.
        /// </summary>
        ValueTask<RequestExecutorFactoryOptions> GetAsync(
            NameString schemaName,
            CancellationToken cancellationToken);

        /// <summary>
        /// Registers a listener to be called whenever a named
        /// <see cref="RequestExecutorFactoryOptions"/> changes.
        /// </summary>
        /// <param name="listener">
        /// The action to be invoked when <see cref="RequestExecutorFactoryOptions"/> has changed.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> which should be disposed to stop listening for changes.
        /// </returns>
        IDisposable OnChange(Action<RequestExecutorFactoryOptions, string> listener);
    }
}
