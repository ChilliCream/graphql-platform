using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching.Processing
{
    public interface IFetchCall
    {
        ValueTask<IQueryResult> InvokeAsync(
            IResolverContext context);
    }
}
