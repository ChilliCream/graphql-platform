using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public class GitHubClient
        : IGitHubClient
    {
        private readonly IOperationExecutor _executor;

        public GitHubClient(IOperationExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public Task<IOperationResult<IGetUser>> GetUserAsync(
            Optional<string> login = default,
            CancellationToken cancellationToken = default)
        {
            if (login.HasValue && login.Value is null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            return _executor.ExecuteAsync(
                new GetUserOperation { Login = login },
                cancellationToken);
        }

        public Task<IOperationResult<IGetUser>> GetUserAsync(
            GetUserOperation operation,
            CancellationToken cancellationToken = default)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return _executor.ExecuteAsync(operation, cancellationToken);
        }
    }
}
