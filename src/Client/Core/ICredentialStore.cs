using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Client
{
    /// <summary>
    /// Abstraction for interacting with credentials
    /// </summary>
    public interface ICredentialStore
    {
        /// <summary>
        /// Retrieve the credentials from the underlying store
        /// </summary>
        /// <param name="cancellationToken">The optional cancellation token to use.</param>
        /// <returns>A continuation containing credentials</returns>
        Task<string> GetCredentials(CancellationToken cancellationToken = default);
    }
}
