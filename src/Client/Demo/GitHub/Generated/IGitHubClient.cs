using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace  StrawberryShake.Client.GitHub
{
    public interface IGitHubClient
    {
        Task<IOperationResult<IGetUser>> GetUserAsync(
            string login);

        Task<IOperationResult<IGetUser>> GetUserAsync(
            string login,
            CancellationToken cancellationToken);
    }
}
