using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace Foo
{
    public interface IStarWarsClient
    {
        Task<IOperationResult<IGetHero>> GetHeroAsync(
            ReviewInput foo);

        Task<IOperationResult<IGetHero>> GetHeroAsync(
            ReviewInput foo,
            CancellationToken cancellationToken);
    }
}
