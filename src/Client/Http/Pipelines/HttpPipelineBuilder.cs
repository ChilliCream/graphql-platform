using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public class HttpPipelineBuilder
        : IHttpPipelineBuilder
    {
        private readonly Stack<HttpOperationMiddleware> _components =
            new Stack<HttpOperationMiddleware>();

        public IHttpPipelineBuilder Use(
            Func<HttpOperationDelegate, HttpOperationDelegate> middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _components.Push((s, n) => middleware(n));
            return this;
        }

        public IHttpPipelineBuilder Use(
            HttpOperationMiddleware middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _components.Push(middleware);
            return this;
        }

        public HttpOperationDelegate Build(IServiceProvider services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (_components.Count == 0)
            {
                throw new InvalidOperationException(
                    "There has to be at least one operation middleware.");
            }

            HttpOperationDelegate next = ThrowExceptionMiddleware;

            while (_components.Count > 0)
            {
                HttpOperationMiddleware middleware = _components.Pop();
                next = middleware.Invoke(services, next);
            }

            return next;
        }

        public static HttpPipelineBuilder New() => new HttpPipelineBuilder();

        private static Task ThrowExceptionMiddleware(
            IHttpOperationContext context)
        {
            if (!context.Result.IsDataOrErrorModified)
            {
                throw new InvalidOperationException(
                    "The operation was not be handled by any middleware.");
            }
            return Task.CompletedTask;
        }
    }
}
