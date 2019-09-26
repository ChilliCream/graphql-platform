using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public interface IHttpOperationExecutorBuilder
    {
        IHttpOperationExecutorBuilder SetClient(Func<IServiceProvider, Func<HttpClient>> clientFactory);

        IHttpOperationExecutorBuilder SetPipeline(Func<IServiceProvider, OperationDelegate> pipelineFactory);

        IHttpOperationExecutorBuilder AddServices(IServiceProvider services);

        IHttpOperationExecutorBuilder AddService(Type serviceType, object serviceInstance);

        IOperationExecutor Build();
    }
}
