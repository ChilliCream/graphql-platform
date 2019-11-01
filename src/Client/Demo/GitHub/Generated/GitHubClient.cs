using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace  StrawberryShake.Client.GitHub
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
            string login) =>
            GetUserAsync(login, CancellationToken.None);

        public Task<IOperationResult<IGetUser>> GetUserAsync(
            string login,
            CancellationToken cancellationToken)
        {
            if (login is null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            return _executor.ExecuteAsync(
                new GetUserOperation {Login = login },
                cancellationToken);
        }
    }
}
