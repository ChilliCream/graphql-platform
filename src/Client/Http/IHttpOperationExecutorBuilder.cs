using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public interface IHttpOperationExecutorBuilder
    {
        IHttpOperationExecutorBuilder SetClient(Func<IServiceProvider, HttpClient> client);

        IHttpOperationExecutorBuilder SetClient(HttpClient client);

        IHttpOperationExecutorBuilder SetPipeline(OperationDelegate pipeline);

        IHttpOperationExecutorBuilder AddServices(IServiceProvider services);

        IHttpOperationExecutorBuilder AddService(Type serviceType, object serviceInstance);

        IOperationExecutor Build();
    }
}
