using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Server
{
    public interface IQueryRequestInterceptor<in TContext>
    {
        Task OnCreateAsync(
            TContext context,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken);
    }
}
