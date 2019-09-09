using System;
using System.Threading.Tasks;

namespace StrawberryShake.Http
{
    public delegate Task OperationDelegate(IHttpOperationContext context);

    public delegate OperationDelegate OperationMiddleware(
        IServiceProvider services,
        OperationDelegate next);

    public interface IHttpOperationExecutorBuilder
    {
        IHttpOperationExecutorBuilder Use(Func<OperationDelegate, OperationDelegate> middleware);
        IHttpOperationExecutorBuilder Use(OperationMiddleware middleware);
    }
}
