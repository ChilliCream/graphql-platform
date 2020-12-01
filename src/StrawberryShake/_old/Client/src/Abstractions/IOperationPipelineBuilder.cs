using System;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public delegate Task OperationDelegate<in T>(T context)
        where T : IOperationContext;

    public delegate OperationDelegate<T> OperationMiddleware<T>(
        IServiceProvider services,
        OperationDelegate<T> next)
        where T : IOperationContext;

    public interface IOperationPipelineBuilder<T> where T : IOperationContext
    {
        IOperationPipelineBuilder<T> Use(Type middleware);

        IOperationPipelineBuilder<T> Use<TMiddleware>();

        IOperationPipelineBuilder<T> Use(OperationMiddleware<T> middleware);

        OperationDelegate<T> Build(IServiceProvider services);
    }
}
