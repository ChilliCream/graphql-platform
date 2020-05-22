using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Options;
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
            return (services, options, next) =>
            {
                TMiddleware middleware = MiddlewareCompiler<TMiddleware>
                    .CompileFactory<RequestDelegate>(
                        (services, next) => CreateFactoryParameterHandlers(options))
                    .Invoke(services, next);

                ClassQueryDelegate<TMiddleware, IRequestContext> compiled =
                    MiddlewareCompiler<TMiddleware>.CompileDelegate<IRequestContext>(
                        (context, middleware) => 
                            CreateDelegateParameterHandlers(context, options));

                return context => compiled(context, middleware);
            };
        }

        private static List<IParameterHandler> CreateFactoryParameterHandlers(
            IRequestExecutorOptionsAccessor options)
        {
            var list = new List<IParameterHandler>();
            AddOptions(list, options);
            return list;
        }

        private static List<IParameterHandler> CreateDelegateParameterHandlers(
            Expression context,
            IRequestExecutorOptionsAccessor options)
        {
            var list = new List<IParameterHandler>();
            AddOptions(list, options);
            list.Add(new ServiceParameterHandler(Expression.Property(context, _services)));
            return list;
        }

        private static void AddOptions(
            IList<IParameterHandler> parameterHandlers,
            IRequestExecutorOptionsAccessor options)
        {
            parameterHandlers.Add(new TypeParameterHandler(
                typeof(IDocumentCacheSizeOptionsAccessor),
                Expression.Constant(options)));
            parameterHandlers.Add(new TypeParameterHandler(
                typeof(IErrorHandlerOptionsAccessor),
                Expression.Constant(options)));
            parameterHandlers.Add(new TypeParameterHandler(
                typeof(IInstrumentationOptionsAccessor),
                Expression.Constant(options)));
            parameterHandlers.Add(new TypeParameterHandler(
                typeof(IRequestExecutorOptionsAccessor),
                Expression.Constant(options)));
            parameterHandlers.Add(new TypeParameterHandler(
                typeof(IValidationOptionsAccessor),
                Expression.Constant(options)));
        }
    }
}
