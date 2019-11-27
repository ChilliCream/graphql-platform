using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Pipelines
{
    public class HttpPipelineBuilder
        : IHttpPipelineBuilder
    {
        private readonly Stack<OperationMiddleware> _components =
            new Stack<OperationMiddleware>();

        public IHttpPipelineBuilder Use(
            Func<OperationDelegate, OperationDelegate> middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _components.Push((s, n) => middleware(n));
            return this;
        }

        public IHttpPipelineBuilder Use(
            OperationMiddleware middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _components.Push(middleware);
            return this;
        }

        public OperationDelegate Build(IServiceProvider services)
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

            OperationDelegate next = ThrowExceptionMiddleware;

            while (_components.Count > 0)
            {
                OperationMiddleware middleware = _components.Pop();
                next = middleware.Invoke(services, next);
            }

            return next;
        }

        public static HttpPipelineBuilder New() =>
            new HttpPipelineBuilder();

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
