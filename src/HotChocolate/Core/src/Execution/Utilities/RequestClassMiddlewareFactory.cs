﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Utilities
{
    internal static class RequestClassMiddlewareFactory
    {
        private static readonly Type _validatorFactory = typeof(IDocumentValidatorFactory);
        private static readonly Type _activator = typeof(IActivator);
        private static readonly Type _errorHandler = typeof(IErrorHandler);
        private static readonly Type _diagnosticEvents = typeof(IDiagnosticEvents);
        private static readonly PropertyInfo _getSchemaName =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.SchemaName))!;

        private static readonly PropertyInfo _requestServices =
            typeof(IRequestContext)
                .GetProperty(nameof(IRequestContext.Services))!;

        private static readonly PropertyInfo _appServices =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.Services))!;

        private static readonly PropertyInfo _schemaServices =
            typeof(IRequestCoreMiddlewareContext)
                .GetProperty(nameof(IRequestCoreMiddlewareContext.SchemaServices))!;

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
                            (c, n) => CreateFactoryParameterHandlers(c, context.Options))
                        .Invoke(context, next);

                ClassQueryDelegate<TMiddleware, IRequestContext> compiled =
                    MiddlewareCompiler<TMiddleware>.CompileDelegate<IRequestContext>(
                        (c, m) => CreateDelegateParameterHandlers(c, context.Options));

                return c => compiled(c, middleware);
            };
        }

        private static List<IParameterHandler> CreateFactoryParameterHandlers(
            Expression context,
            IRequestExecutorOptionsAccessor options)
        {
            Expression schemaName = Expression.Property(context, _getSchemaName);
            Expression services = Expression.Property(context, _appServices);
            Expression schemaServices = Expression.Property(context, _schemaServices);
            Expression validatorFactory = Expression.Convert(Expression.Call(
                services, _getService, Expression.Constant(_validatorFactory)), _validatorFactory);
            Expression getValidator = Expression.Call(
                validatorFactory, _createValidator, schemaName);

            var list = new List<IParameterHandler>();
            list.Add(new TypeParameterHandler(typeof(IDocumentValidator), getValidator));
            AddService<IActivator>(list, schemaServices);
            AddService<IErrorHandler>(list, schemaServices);
            AddService<IDiagnosticEvents>(list, schemaServices);
            AddService<QueryExecutor>(list, schemaServices);
            AddService<MutationExecutor>(list, schemaServices);
            AddService<SubscriptionExecutor>(list, schemaServices);
            AddOptions(list, options);
            list.Add(new SchemaNameParameterHandler(schemaName));
            list.Add(new ServiceParameterHandler(services));
            return list;
        }

        private static void AddService<T>(
            ICollection<IParameterHandler> parameterHandlers,
            Expression service)
        {
            Expression expression = Expression.Convert(Expression.Call(
                service, _getService, Expression.Constant(typeof(T))), typeof(T));
            parameterHandlers.Add(new TypeParameterHandler(typeof(T), expression));
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
        }

        private class SchemaNameParameterHandler : IParameterHandler
        {
            private Expression _schemaName;

            public SchemaNameParameterHandler(Expression schemaName)
            {
                _schemaName = schemaName;
            }

            public bool CanHandle(ParameterInfo parameter)
            {
                return parameter.ParameterType == typeof(NameString) &&
                    parameter.Name == "schemaName";
            }

            public Expression CreateExpression(ParameterInfo parameter)
            {
                return _schemaName;
            }
        }
    }
}
