using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class SocketSessionInterceptorMock : DefaultSocketSessionInterceptor
    {
        public override ValueTask OnRequestAsync(
            ISocketConnection connection,
            IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            return default;
        }
    }
}
