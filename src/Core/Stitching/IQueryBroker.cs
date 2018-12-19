using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Stitching
{
    public interface IQueryBroker
    {
        Task<IExecutionResult> RedirectQueryAsync(
            IDirectiveContext directiveContext);
    }
}
