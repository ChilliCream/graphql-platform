using System;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public delegate Task HttpOperationDelegate(IHttpOperationContext context);

    public delegate HttpOperationDelegate HttpOperationMiddleware(
        IServiceProvider services,
        HttpOperationDelegate next);

    public interface IHttpPipelineBuilder
    {
        IHttpPipelineBuilder Use(Func<HttpOperationDelegate, HttpOperationDelegate> middleware);

        IHttpPipelineBuilder Use(HttpOperationMiddleware middleware);

        HttpOperationDelegate Build(IServiceProvider services);
    }
}
