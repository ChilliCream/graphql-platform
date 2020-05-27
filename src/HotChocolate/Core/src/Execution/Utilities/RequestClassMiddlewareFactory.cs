using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Options;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Utilities
{
    internal static class RequestClassMiddlewareFactory
    {
        private static readonly Type _validatorFactory = typeof(IDocumentValidatorFactory);

        private static readonly PropertyInfo _getConfigName =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.Name))!;

        private static readonly PropertyInfo _requestServices =
            typeof(IRequestContext)
                .GetProperty(nameof(IRequestContext.Services))!;

        private static readonly PropertyInfo _appServices =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.Services))!;

        private static readonly PropertyInfo _activator =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.Activator))!;

        private static readonly PropertyInfo _errorHandler =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.ErrorHandler))!;

        private static readonly MethodInfo _getService =
            typeof(IServiceProvider)
                .GetMethod(nameof(IServiceProvider.GetService))!;

        private static readonly MethodInfo _createValidator =
            typeof(IDocumentValidatorFactory)
                .GetMethod(nameof(IDocumentValidatorFactory.CreateValidator))!;

        internal static RequestCoreMiddleware Create<TMiddleware>()
            where TMiddleware : class
        {
            return (context, next) =>
            {
                TMiddleware middleware =
                    MiddlewareCompiler<TMiddleware>
                        .CompileFactory<IRequestCoreMiddlewareContext, RequestDelegate>(
                            (ctx, next) => CreateFactoryParameterHandlers(ctx, context.Options))
                        .Invoke(context, next);

                ClassQueryDelegate<TMiddleware, IRequestContext> compiled =
                    MiddlewareCompiler<TMiddleware>.CompileDelegate<IRequestContext>(
                        (ctx, middleware) => CreateDelegateParameterHandlers(ctx, context.Options));

                return context => compiled(context, middleware);
            };
        }

        private static List<IParameterHandler> CreateFactoryParameterHandlers(
            Expression context,
            IRequestExecutorOptionsAccessor options)
        {
            Expression configName = Expression.Property(context, _getConfigName);
            Expression services = Expression.Property(context, _appServices);
            Expression activator = Expression.Property(context, _activator);
            Expression errorHandler = Expression.Property(context, _errorHandler);
            Expression validatorFactory = Expression.Convert(Expression.Call(
                services, _getService, Expression.Constant(_validatorFactory)), _validatorFactory);
            Expression getValidator = Expression.Call(
                validatorFactory, _createValidator, configName);

            var list = new List<IParameterHandler>();
            list.Add(new TypeParameterHandler(typeof(IDocumentValidator), getValidator));
            list.Add(new TypeParameterHandler(typeof(IActivator), activator));
            list.Add(new TypeParameterHandler(typeof(IErrorHandler), errorHandler));
            AddOptions(list, options);
            list.Add(new ConfigNameParameterHandler(configName));
            list.Add(new ServiceParameterHandler(services));
            return list;
        }

        private static List<IParameterHandler> CreateDelegateParameterHandlers(
            Expression context,
            IRequestExecutorOptionsAccessor options)
        {
            var list = new List<IParameterHandler>();
            AddOptions(list, options);
            list.Add(new ServiceParameterHandler(Expression.Property(context, _requestServices)));
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

        private class ConfigNameParameterHandler : IParameterHandler
        {
            private Expression _configName;

            public ConfigNameParameterHandler(Expression configName)
            {
                _configName = configName;
            }

            public bool CanHandle(ParameterInfo parameter)
            {
                return parameter.ParameterType == typeof(string) &&
                    parameter.Name == "configName";
            }

            public Expression CreateExpression(ParameterInfo parameter)
            {
                return _configName;
            }
        }
    }
}
