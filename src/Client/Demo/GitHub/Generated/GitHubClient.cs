using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    [System.CodeDom.Compiler.GeneratedCode("StrawberryShake", "11.0.0")]
    public class GitHubClient
        : IGitHubClient
    {
        private const string _clientName = "GitHubClient";

        private readonly IOperationExecutor _executor;

        public GitHubClient(IOperationExecutorPool executorPool)
        {
            _executor = executorPool.CreateExecutor(_clientName);
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
