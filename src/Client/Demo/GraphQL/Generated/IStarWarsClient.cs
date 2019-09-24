using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace StrawberryShake.Client
{
    public interface IStarWarsClient
    {
        Task<IOperationResult<IGetHero>> GetHeroAsync();

        Task<IOperationResult<IGetHero>> GetHeroAsync(
            CancellationToken cancellationToken);
    }
}
