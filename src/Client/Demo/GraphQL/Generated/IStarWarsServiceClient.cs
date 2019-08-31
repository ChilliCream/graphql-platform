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
}
