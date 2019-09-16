using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public interface IHttpOperationExecutorBuilder
    {
        IHttpOperationExecutorBuilder SetClient(HttpClient client);

        IHttpOperationExecutorBuilder AddServices(IServiceProvider services);

        IHttpOperationExecutorBuilder AddService(Type serviceType, object serviceInstance);

        IHttpOperationExecutorBuilder Use(Func<OperationDelegate, OperationDelegate> middleware);

        IHttpOperationExecutorBuilder Use(OperationMiddleware middleware);

        IOperationExecutor Build();
    }
}
