using System;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public delegate Task OperationDelegate(IHttpOperationContext context);

    public delegate OperationDelegate OperationMiddleware(
        IServiceProvider services,
        OperationDelegate next);

    public interface IHttpPipelineBuilder
    {
        IHttpPipelineBuilder Use(Func<OperationDelegate, OperationDelegate> middleware);
        IHttpPipelineBuilder Use(OperationMiddleware middleware);
        OperationDelegate Build(IServiceProvider services);
    }
}
