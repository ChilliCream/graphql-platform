using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorBuilder
        : IHttpOperationExecutorBuilder
    {
        public IHttpOperationExecutorBuilder AddService(Type serviceType, object serviceInstance)
        {
            throw new NotImplementedException();
        }

        public IHttpOperationExecutorBuilder AddServices(IServiceProvider services)
        {
            throw new NotImplementedException();
        }

        public IOperationExecutor Build()
        {
            throw new NotImplementedException();
        }

        public IHttpOperationExecutorBuilder SetClient(HttpClient client)
        {
            throw new NotImplementedException();
        }

        public IHttpOperationExecutorBuilder Use(Func<OperationDelegate, OperationDelegate> middleware)
        {
            throw new NotImplementedException();
        }

        public IHttpOperationExecutorBuilder Use(OperationMiddleware middleware)
        {
            throw new NotImplementedException();
        }
    }

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
