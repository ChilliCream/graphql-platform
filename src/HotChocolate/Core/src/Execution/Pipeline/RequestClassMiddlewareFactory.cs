using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Processing;
using HotChocolate.Utilities;
using HotChocolate.Validation;

namespace HotChocolate.Execution.Pipeline;

internal static class RequestClassMiddlewareFactory
{
    private static readonly Type _validatorFactory = typeof(IDocumentValidatorFactory);

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
            var middleware =
                MiddlewareCompiler<TMiddleware>
                    .CompileFactory<IRequestCoreMiddlewareContext, RequestDelegate>(
                        (c, _) => CreateFactoryParameterHandlers(
                            c, context.Options, typeof(TMiddleware)))
                    .Invoke(context, next);

            var compiled =
                MiddlewareCompiler<TMiddleware>.CompileDelegate<IRequestContext>(
                    (c, _) => CreateDelegateParameterHandlers(c, context.Options));

            return c => compiled(c, middleware);
        };
    }

    private static List<IParameterHandler> CreateFactoryParameterHandlers(
        Expression context,
        IRequestExecutorOptionsAccessor options,
        Type middleware)
    {
        Expression schemaName = Expression.Property(context, _getSchemaName);
        Expression services = Expression.Property(context, _appServices);
        Expression schemaServices = Expression.Property(context, _schemaServices);

        Expression validatorFactory =
            Expression.Convert(
                Expression.Call(
                    services,
                    _getService,
                    Expression.Constant(_validatorFactory)),
                _validatorFactory);

        Expression getValidator =
            Expression.Call(
                validatorFactory,
                _createValidator,
                schemaName);

        var list = new List<IParameterHandler> { new TypeParameterHandler(typeof(IDocumentValidator), getValidator) };

        var constructor = middleware.GetConstructors().SingleOrDefault(t => t.IsPublic);

        if (constructor is not null)
        {
            foreach (var parameter in constructor.GetParameters()
                .Where(p => p.IsDefined(typeof(SchemaServiceAttribute))))
            {
                AddService(list, schemaServices, parameter.ParameterType);
            }
        }

        AddService<IErrorHandler>(list, schemaServices);
        AddService<IExecutionDiagnosticEvents>(list, schemaServices);
        AddService<IOperationDocumentStorage>(list, schemaServices);
        AddService<QueryExecutor>(list, schemaServices);
        AddService<IEnumerable<IOperationCompilerOptimizer>>(list, schemaServices);
        AddService<SubscriptionExecutor>(list, schemaServices);
        AddOptions(list, options);
        list.Add(new SchemaNameParameterHandler(schemaName));
        list.Add(new ServiceParameterHandler(services));
        return list;
    }

    private static void AddService<T>(
        ICollection<IParameterHandler> parameterHandlers,
        Expression service) =>
        AddService(parameterHandlers, service, typeof(T));

    private static void AddService(
        ICollection<IParameterHandler> parameterHandlers,
        Expression service,
        Type serviceType)
    {
        Expression serviceTypeConst = Expression.Constant(serviceType);
        Expression getService = Expression.Call(service, _getService, serviceTypeConst);
        Expression castService = Expression.Convert(getService, serviceType);
        parameterHandlers.Add(new TypeParameterHandler(serviceType, castService));
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
            typeof(IErrorHandlerOptionsAccessor),
            Expression.Constant(options)));
        parameterHandlers.Add(new TypeParameterHandler(
            typeof(IRequestExecutorOptionsAccessor),
            Expression.Constant(options)));
        parameterHandlers.Add(new TypeParameterHandler(
            typeof(IPersistedOperationOptionsAccessor),
            Expression.Constant(options)));
    }

    private sealed class SchemaNameParameterHandler : IParameterHandler
    {
        private readonly Expression _schemaName;

        public SchemaNameParameterHandler(Expression schemaName)
        {
            _schemaName = schemaName;
        }

        public bool CanHandle(ParameterInfo parameter)
        {
            return parameter.ParameterType == typeof(string) && parameter.Name == "schemaName";
        }

        public Expression CreateExpression(ParameterInfo parameter)
        {
            return _schemaName;
        }
    }
}
