using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Utilities
{
    internal static class RequestClassMiddlewareFactory
    {
        private static PropertyInfo _services =
            typeof(IRequestContext).GetProperty(nameof(IRequestContext.Services))!;

        internal static RequestServicesMiddleware Create<TMiddleware>()
            where TMiddleware : class
        {
            return (services, next) =>
            {
                TMiddleware middleware = MiddlewareCompiler<TMiddleware>
                    .CompileFactory<RequestDelegate>()
                    .Invoke(services, next);

                ClassQueryDelegate<TMiddleware, IRequestContext> compiled =
                    MiddlewareCompiler<TMiddleware>.CompileDelegate<IRequestContext>(
                        (context, middleware) =>
                            new List<IParameterHandler>
                            {
                                new ServiceParameterHandler(Expression.Property(context, _services))
                            });

                return context => compiled(context, middleware);
            };
        }
    }
}
