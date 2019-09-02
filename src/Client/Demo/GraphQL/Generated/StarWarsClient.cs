using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace Foo
{
    public class StarWarsClient
        : IStarWarsClient
    {
        private readonly IOperationExecutor _executor;

        public StarWarsClient(IOperationExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public Task<IOperationResult<IGetHero>> GetHeroAsync(
            ReviewInput foo) =>
            GetHeroAsync(foo, CancellationToken.None);

        public Task<IOperationResult<IGetHero>> GetHeroAsync(
            ReviewInput foo,
            CancellationToken cancellationToken)
        {
            if (foo is null)
            {
                throw new ArgumentNullException(nameof(foo));
            }

            return _executor.ExecuteAsync(
                new GetHeroOperation {Foo = foo },
                cancellationToken);
        }
    }
}
