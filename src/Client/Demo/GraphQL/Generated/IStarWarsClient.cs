using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace  StrawberryShake.Client.GraphQL
{
    public interface IStarWarsClient
    {
        Task<IOperationResult<IGetHero>> GetHeroAsync(
            Episode? episode);

        Task<IOperationResult<IGetHero>> GetHeroAsync(
            Episode? episode,
            CancellationToken cancellationToken);
    }
}
