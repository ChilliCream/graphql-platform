using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public class OperationPipelineBuilder<T>
        : IOperationPipelineBuilder<T>
        where T : IOperationContext
    {
        private readonly Stack<OperationMiddleware<T>> _components =
            new Stack<OperationMiddleware<T>>();

        public IOperationPipelineBuilder<T> Use(Type middleware)
        {
            return Use(ClassMiddlewareFactory<T>.Create(middleware));
        }

        public IOperationPipelineBuilder<T> Use<TMiddleware>()
        {
            return Use(ClassMiddlewareFactory<T>.Create(typeof(TMiddleware)));
        }

        public IOperationPipelineBuilder<T> Use(OperationMiddleware<T> middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _components.Push(middleware);
            return this;
        }

        public OperationDelegate<T> Build(IServiceProvider services)
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

            OperationDelegate<T> next = ThrowExceptionMiddlewareAsync;

            while (_components.Count > 0)
            {
                OperationMiddleware<T> middleware = _components.Pop();
                next = middleware.Invoke(services, next);
            }

            return next;
        }

        public static OperationPipelineBuilder<T> New() =>
            new OperationPipelineBuilder<T>();

        private static Task ThrowExceptionMiddlewareAsync(T context)
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
