using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client.GitHub
{
    public interface IGitHubClient
    {
        Task<IOperationResult<IGetUser>> GetUserAsync(
            Optional<string> login = default,
            CancellationToken cancellationToken = default);

        Task<IOperationResult<IGetUser>> GetUserAsync(
            GetUserOperation operation,
            CancellationToken cancellationToken = default);
    }
}
