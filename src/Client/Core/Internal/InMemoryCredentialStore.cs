using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Client.Internal
{
    /// <summary>
    /// An in memory credential store
    /// </summary>
    public class InMemoryCredentialStore : ICredentialStore
    {
        readonly string token;

        /// <summary>
        /// Construct an in memory credential store
        /// </summary>
        /// <param name="token">The token to return</param>
        public InMemoryCredentialStore(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException(nameof(token));
            }

            this.token = token;
        }

        /// <inheritdoc />
        public Task<string> GetCredentials(CancellationToken cancellationToken = default) => Task.FromResult(token);
    }
}
