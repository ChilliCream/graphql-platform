using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;

namespace Mocha;

internal sealed class MessagingPools(
    ObjectPool<DispatchContext> dispatchContextPool,
    ObjectPool<ReceiveContext> receiveContextPool) : IMessagingPools
{
    public ObjectPool<DispatchContext> DispatchContext => dispatchContextPool;
    public ObjectPool<ReceiveContext> ReceiveContext => receiveContextPool;
}
