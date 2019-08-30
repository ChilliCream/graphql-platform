using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake;

namespace Foo
{
    public interface IStarWarsServiceClient
    {
        Task<IOperationResult<IGetHero>> GetHeroAsync(
            ReviewInput foo);

        Task<IOperationResult<IGetHero>> GetHeroAsync(
            ReviewInput foo,
            CancellationToken cancellationToken);
    }

    public class StarWarsServiceClient
        : IStarWarsServiceClient
    {
        private IOperationExecutor _executor;

        public StarWarsServiceClient(IOperationExecutor executor)
        {
            if (executor is null)
            {
                throw new ArgumentNullException(nameof(executor));
            }

            _executor = executor;
        }

        public Task<IOperationResult<IGetHero>> GetHeroAsync(ReviewInput foo) =>
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
                new GetHeroOperation { Foo = foo },
                cancellationToken);
        }
    }
}
