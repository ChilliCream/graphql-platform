using System;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public delegate Task OperationDelegate(IHttpOperationContext context);

    public delegate OperationDelegate OperationMiddleware(
        IServiceProvider services,
        OperationDelegate next);

    public interface IHttpOperationExecutionPipelineBuilder
    {
        IHttpOperationExecutionPipelineBuilder Use(Func<OperationDelegate, OperationDelegate> middleware);
        IHttpOperationExecutionPipelineBuilder Use(OperationMiddleware middleware);
    }
}
